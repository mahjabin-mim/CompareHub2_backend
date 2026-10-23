using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Infrastructure.Persistence;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.DTOs;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.Interfaces;

namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public class PriceCheckService : IPriceCheckService
{
    // Be gentle with the stores: a handful of searches at a time.
    private const int MaxParallelStoreSearches = 6;

    // Only one run at a time, even if the scheduler fires twice or someone triggers it by hand.
    private static readonly SemaphoreSlim RunGate = new(1, 1);

    private readonly AppDbContext _db;
    private readonly IProductScraperService _scraper;
    private readonly IAlertNotifier _notifier;
    private readonly ILogger<PriceCheckService> _logger;

    public PriceCheckService(AppDbContext db, IProductScraperService scraper, IAlertNotifier notifier, ILogger<PriceCheckService> logger)
    {
        _db = db;
        _scraper = scraper;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<PriceCheckSummary> RunAsync(CancellationToken cancellationToken)
    {
        if (!await RunGate.WaitAsync(0, cancellationToken))
        {
            return new PriceCheckSummary(0, 0, 0, 0, 0, AlreadyRunning: true);
        }

        try
        {
            return await CheckAllAsync(cancellationToken);
        }
        finally
        {
            RunGate.Release();
        }
    }

    private async Task<PriceCheckSummary> CheckAllAsync(CancellationToken cancellationToken)
    {
        var items = await _db.WishlistItems
            .AsSplitQuery()
            .Include(x => x.User)
            .Include(x => x.Offers)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return new PriceCheckSummary(0, 0, 0, 0, 0);
        }

        var searches = await SearchStoresAsync(items, cancellationToken);

        var now = DateTime.UtcNow;
        var drops = new List<(AppUser User, PriceAlert Alert, PriceDropNotice Notice)>();
        var offersChecked = 0;
        var pricesChanged = 0;

        foreach (var item in items)
        {
            item.LastCheckedAt = now;
            foreach (var offer in item.Offers)
            {
                var found = searches.GetValueOrDefault((offer.StoreKey, item.Name));
                var match = found?.FirstOrDefault(result => PriceRules.SameListing(result.ProductUrl, offer.ProductUrl));
                if (match is null)
                {
                    // Store didn't answer or the product wasn't in the results; keep the last known price.
                    continue;
                }

                offersChecked++;
                offer.LastSeenAt = now;
                if (match.Price == offer.CurrentPrice || !PriceRules.IsPlausibleChange(offer.CurrentPrice, match.Price))
                {
                    continue;
                }

                var oldPrice = offer.CurrentPrice;
                offer.CurrentPrice = match.Price;
                // Added to the context explicitly: a new entity with a preset id reached through an
                // already-tracked offer would otherwise be treated as an existing row and fail to update.
                _db.PricePoints.Add(new PricePoint { WishlistOfferId = offer.Id, Price = match.Price, RecordedAt = now });
                pricesChanged++;

                if (PriceRules.IsMeaningfulDrop(oldPrice, match.Price))
                {
                    var alert = new PriceAlert
                    {
                        UserId = item.UserId,
                        WishlistItemId = item.Id,
                        WishlistOfferId = offer.Id,
                        StoreKey = offer.StoreKey,
                        OldPrice = oldPrice,
                        NewPrice = match.Price,
                        CreatedAt = now
                    };
                    drops.Add((item.User, alert, new PriceDropNotice(item.Name, offer.StoreKey, oldPrice, match.Price, offer.ProductUrl)));
                }
            }
        }

        _db.PriceAlerts.AddRange(drops.Select(x => x.Alert));
        await _db.SaveChangesAsync(cancellationToken);

        var usersNotified = 0;
        foreach (var userDrops in drops.GroupBy(x => x.User.Id))
        {
            try
            {
                await _notifier.NotifyAsync(userDrops.First().User, userDrops.Select(x => x.Notice).ToList(), cancellationToken);
                usersNotified++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not notify user {UserId} about price drops.", userDrops.Key);
            }
        }

        _logger.LogInformation(
            "Price check: {Items} products, {Checked} offers rechecked, {Changed} prices changed, {Alerts} drops, {Users} users notified.",
            items.Count, offersChecked, pricesChanged, drops.Count, usersNotified);
        return new PriceCheckSummary(items.Count, offersChecked, pricesChanged, drops.Count, usersNotified);
    }

    // One search per (store, product name), shared by everyone who saved that product.
    private async Task<IReadOnlyDictionary<(string Store, string Name), List<ProductResultDto>>> SearchStoresAsync(
        List<WishlistItem> items, CancellationToken cancellationToken)
    {
        var lookups = items.SelectMany(item => item.Offers.Select(offer => (Store: offer.StoreKey, item.Name))).Distinct().ToList();
        var results = new ConcurrentDictionary<(string Store, string Name), List<ProductResultDto>>();
        using var gate = new SemaphoreSlim(MaxParallelStoreSearches);

        await Task.WhenAll(lookups.Select(async lookup =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                // The scraper picks the right store handler from the host, and ignores hosts it doesn't know.
                results[lookup] = await _scraper.SearchProductsAsync($"https://{lookup.Store}/", lookup.Name, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Price check search failed for {Store}.", lookup.Store);
            }
            finally
            {
                gate.Release();
            }
        }));

        return results;
    }
}
