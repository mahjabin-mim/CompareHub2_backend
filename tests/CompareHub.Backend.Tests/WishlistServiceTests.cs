using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CompareHub.Backend.app.Core.Modules.Wishlist.DTOs;
using CompareHub.Backend.app.Core.Modules.Wishlist.Services;

namespace CompareHub.Backend.Tests;

public class WishlistServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeCurrentUser _currentUser = new();

    public void Dispose() => _database.Dispose();

    private WishlistService ServiceFor(Guid userId)
    {
        _currentUser.UserId = userId;
        return new WishlistService(_database.CreateContext(), _currentUser);
    }

    private static AddWishlistItemRequestDto Serum(string key = "nineless|30ml", params (string Store, decimal Price)[] offers) =>
        new(key, "Nineless B-Boost Serum 30ml", ["https://img.example/a.jpg"],
            (offers.Length > 0 ? offers : [("skinnora.com", 1090m), ("kireibd.com", 1480m)])
                .Select(o => new WishlistOfferInputDto(o.Store, o.Price, $"https://{o.Store}/p/nineless-serum")).ToList());

    [Fact]
    public async Task Add_SavesEveryStoreOfferWithItsFirstPricePoint()
    {
        var user = _database.AddUser();

        var saved = await ServiceFor(user.Id).AddAsync(Serum(), default);

        Assert.Equal(2, saved.Offers.Count);
        Assert.Equal([1090m, 1480m], saved.Offers.Select(o => o.CurrentPrice));
        Assert.All(saved.Offers, offer => Assert.Single(offer.History));
        Assert.Equal(["https://img.example/a.jpg"], saved.ImageUrls);
    }

    [Fact]
    public async Task Add_TheSameProductAgainReturnsTheSavedOneEvenIfItsKeyChanged()
    {
        var user = _database.AddUser();
        var first = await ServiceFor(user.Id).AddAsync(Serum("key-one"), default);

        var again = await ServiceFor(user.Id).AddAsync(Serum("key-two"), default);

        Assert.Equal(first.Id, again.Id);
        Assert.Single(await ServiceFor(user.Id).ListAsync(default));
    }

    [Fact]
    public async Task Add_DropsUnsafeLinksAndRejectsAProductWithNothingValid()
    {
        var user = _database.AddUser();
        var request = new AddWishlistItemRequestDto("k", "Serum", ["javascript:alert(1)", "https://img.example/ok.jpg"],
        [
            new("kireibd.com", 100, "javascript:alert(1)"),
            new("arogga.com", 0, "https://arogga.com/p/1"),
            new("skinnora.com", 500, "https://skinnora.com/p/1")
        ]);

        var saved = await ServiceFor(user.Id).AddAsync(request, default);

        Assert.Equal(["skinnora.com"], saved.Offers.Select(o => o.StoreKey));
        Assert.Equal(["https://img.example/ok.jpg"], saved.ImageUrls);
        await Assert.ThrowsAsync<ValidationException>(() =>
            ServiceFor(user.Id).AddAsync(new AddWishlistItemRequestDto("k2", "Bad", null, [new("x", 100, "ftp://x/y")]), default));
        await Assert.ThrowsAsync<ValidationException>(() =>
            ServiceFor(user.Id).AddAsync(new AddWishlistItemRequestDto("", "No key", null, [new("x", 100, "https://x.com/y")]), default));
    }

    [Fact]
    public async Task Add_StopsAtTheWishlistLimit()
    {
        var user = _database.AddUser();
        var service = ServiceFor(user.Id);
        for (var i = 0; i < 100; i++)
        {
            await service.AddAsync(new AddWishlistItemRequestDto($"k{i}", $"Product {i}", null, [new("kireibd.com", 100 + i, $"https://kireibd.com/p/{i}")]), default);
        }

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddAsync(new AddWishlistItemRequestDto("one-more", "One more", null, [new("kireibd.com", 100, "https://kireibd.com/p/new")]), default));
    }

    [Fact]
    public async Task EachUserOnlySeesAndRemovesTheirOwnProducts()
    {
        var alice = _database.AddUser("alice@example.com");
        var bob = _database.AddUser("bob@example.com");
        var aliceItem = await ServiceFor(alice.Id).AddAsync(Serum(), default);

        Assert.Empty(await ServiceFor(bob.Id).ListAsync(default));
        await Assert.ThrowsAsync<ValidationException>(() => ServiceFor(bob.Id).RemoveAsync(aliceItem.Id, default));
        Assert.Single(await ServiceFor(alice.Id).ListAsync(default));

        await ServiceFor(alice.Id).RemoveAsync(aliceItem.Id, default);
        Assert.Empty(await ServiceFor(alice.Id).ListAsync(default));
        using var context = _database.CreateContext();
        Assert.Empty(await context.WishlistOffers.ToListAsync());
        Assert.Empty(await context.PricePoints.ToListAsync());
    }

    [Fact]
    public async Task Import_MovesTheDeviceWishlistInAndSkipsEntriesThatAreInvalidOrAlreadySaved()
    {
        var user = _database.AddUser();
        await ServiceFor(user.Id).AddAsync(Serum("existing"), default);

        var imported = await ServiceFor(user.Id).ImportAsync(new ImportWishlistRequestDto(
        [
            Serum("existing"),
            new AddWishlistItemRequestDto("", "broken", null, []),
            new AddWishlistItemRequestDto("toner", "Anua Toner", null, [new("arogga.com", 900, "https://arogga.com/p/toner")])
        ]), default);

        Assert.Equal(2, imported.Count);
        Assert.Equal(2, (await ServiceFor(user.Id).ListAsync(default)).Count);
    }

    [Fact]
    public async Task Alerts_AreListedNewestFirstCountedAsUnreadAndMarkedRead()
    {
        var user = _database.AddUser();
        var other = _database.AddUser("other@example.com");
        var item = await ServiceFor(user.Id).AddAsync(Serum(), default);
        using (var context = _database.CreateContext())
        {
            var offerId = item.Offers[0].Id;
            context.PriceAlerts.AddRange(
                new app.Core.Domain.Entities.PriceAlert { UserId = user.Id, WishlistItemId = item.Id, WishlistOfferId = offerId, StoreKey = "skinnora.com", OldPrice = 1200, NewPrice = 1090, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                new app.Core.Domain.Entities.PriceAlert { UserId = user.Id, WishlistItemId = item.Id, WishlistOfferId = offerId, StoreKey = "skinnora.com", OldPrice = 1090, NewPrice = 990, CreatedAt = DateTime.UtcNow },
                new app.Core.Domain.Entities.PriceAlert { UserId = other.Id, WishlistItemId = item.Id, WishlistOfferId = offerId, StoreKey = "x", OldPrice = 5, NewPrice = 4 });
            await context.SaveChangesAsync();
        }

        var alerts = await ServiceFor(user.Id).GetAlertsAsync(default);

        Assert.Equal(2, alerts.UnreadCount);
        Assert.Equal([990m, 1090m], alerts.Alerts.Select(a => a.NewPrice));
        Assert.Equal("Nineless B-Boost Serum 30ml", alerts.Alerts[0].ItemName);

        await ServiceFor(user.Id).MarkAlertsReadAsync(default);
        Assert.Equal(0, (await ServiceFor(user.Id).GetAlertsAsync(default)).UnreadCount);
        Assert.Equal(1, (await ServiceFor(other.Id).GetAlertsAsync(default)).UnreadCount);
    }
}
