using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Infrastructure.Auth;
using CompareHub.Backend.app.Core.Infrastructure.Persistence;
using CompareHub.Backend.app.Core.Modules.Alerts.Services;
using CompareHub.Backend.app.Core.Modules.Wishlist.DTOs;
using CompareHub.Backend.app.Core.Modules.Wishlist.Interfaces;

namespace CompareHub.Backend.app.Core.Modules.Wishlist.Services;

public class WishlistService : IWishlistService
{
    private const int MaxItemsPerUser = 100;
    private const int MaxOffersPerItem = 25;
    private const int MaxImagesPerItem = 3;
    private const int MaxHistoryPointsPerOffer = 90;
    private const int MaxAlertsReturned = 30;
    private const decimal MaxPrice = 10_000_000m;

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WishlistService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<WishlistItemDto>> ListAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        var items = await _db.WishlistItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Offers).ThenInclude(x => x.History)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToList();
    }

    public async Task<WishlistItemDto> AddAsync(AddWishlistItemRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        var existing = await _db.WishlistItems
            .Include(x => x.Offers).ThenInclude(x => x.History)
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var item = AddIfNew(userId, request, existing);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<List<WishlistItemDto>> ImportAsync(ImportWishlistRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        var existing = await _db.WishlistItems
            .Include(x => x.Offers).ThenInclude(x => x.History)
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var added = new List<WishlistItem>();
        foreach (var requested in (request.Items ?? []).Take(MaxItemsPerUser))
        {
            // Skip products that are not valid or no longer fit, so one bad entry doesn't lose the rest.
            try
            {
                added.Add(AddIfNew(userId, requested, existing));
            }
            catch (ValidationException)
            {
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return added.Select(ToDto).ToList();
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        var item = await _db.WishlistItems.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new ValidationException("Wishlist item not found.");

        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AlertsResponseDto> GetAlertsAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        var alerts = await _db.PriceAlerts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxAlertsReturned)
            .Select(x => new PriceAlertDto(x.Id, x.WishlistItemId, x.Item.Name, x.StoreKey, x.OldPrice, x.NewPrice, x.CreatedAt, x.ReadAt != null))
            .ToListAsync(cancellationToken);
        var unread = await _db.PriceAlerts.CountAsync(x => x.UserId == userId && x.ReadAt == null, cancellationToken);

        return new AlertsResponseDto(alerts, unread);
    }

    public async Task MarkAlertsReadAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        await _db.PriceAlerts
            .Where(x => x.UserId == userId && x.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, DateTime.UtcNow), cancellationToken);
    }

    // Returns the already-saved product when the request is the same product (same store link),
    // since the frontend's grouping key can shift as more stores answer.
    private WishlistItem AddIfNew(Guid userId, AddWishlistItemRequestDto request, List<WishlistItem> existing)
    {
        var offers = Validate(request);
        var requestedUrls = offers.Select(x => PriceRules.NormalizeUrl(x.ProductUrl)).ToHashSet();
        var duplicate = existing.FirstOrDefault(item =>
            item.ProductKey == request.Key || item.Offers.Any(offer => requestedUrls.Contains(PriceRules.NormalizeUrl(offer.ProductUrl))));
        if (duplicate is not null)
        {
            return duplicate;
        }

        if (existing.Count >= MaxItemsPerUser)
        {
            throw new ValidationException($"Your wishlist is full ({MaxItemsPerUser} products). Remove one to add another.");
        }

        var now = DateTime.UtcNow;
        var item = new WishlistItem
        {
            UserId = userId,
            ProductKey = request.Key.Trim(),
            Name = request.Name.Trim(),
            ImageUrlsJson = JsonSerializer.Serialize(
                (request.ImageUrls ?? []).Where(IsHttpUrl).Take(MaxImagesPerItem).ToList()),
            CreatedAt = now,
            Offers = offers.Select(offer => new WishlistOffer
            {
                StoreKey = offer.StoreKey.Trim().ToLowerInvariant(),
                ProductUrl = offer.ProductUrl.Trim(),
                SavedPrice = offer.Price,
                CurrentPrice = offer.Price,
                LastSeenAt = now,
                History = { new PricePoint { Price = offer.Price, RecordedAt = now } }
            }).ToList()
        };

        _db.WishlistItems.Add(item);
        existing.Add(item);
        return item;
    }

    private static List<WishlistOfferInputDto> Validate(AddWishlistItemRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || request.Key.Length > 300 ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 300)
        {
            throw new ValidationException("This product can't be saved.");
        }

        var offers = (request.Offers ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.StoreKey) && x.StoreKey.Length <= 120 && IsHttpUrl(x.ProductUrl) && x.ProductUrl.Length <= 1024)
            .Where(x => x.Price > 0 && x.Price <= MaxPrice)
            .DistinctBy(x => PriceRules.NormalizeUrl(x.ProductUrl))
            .Take(MaxOffersPerItem)
            .ToList();
        if (offers.Count == 0)
        {
            throw new ValidationException("This product can't be saved.");
        }

        return offers;
    }

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static WishlistItemDto ToDto(WishlistItem item) => new(
        item.Id,
        item.ProductKey,
        item.Name,
        JsonSerializer.Deserialize<List<string>>(item.ImageUrlsJson) ?? [],
        item.CreatedAt,
        item.LastCheckedAt,
        item.Offers
            .OrderBy(offer => offer.CurrentPrice)
            .Select(offer => new WishlistOfferDto(
                offer.Id,
                offer.StoreKey,
                offer.ProductUrl,
                offer.SavedPrice,
                offer.CurrentPrice,
                offer.LastSeenAt,
                offer.History
                    .OrderByDescending(point => point.RecordedAt)
                    .Take(MaxHistoryPointsPerOffer)
                    .OrderBy(point => point.RecordedAt)
                    .Select(point => new PricePointDto(point.Price, point.RecordedAt))
                    .ToList()))
            .ToList());
}
