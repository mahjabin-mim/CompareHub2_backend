using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;
using CompareHub.Backend.app.Core.Modules.Alerts.Services;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.DTOs;
using CompareHub.Backend.app.Core.Modules.Wishlist.DTOs;
using CompareHub.Backend.app.Core.Modules.Wishlist.Services;

namespace CompareHub.Backend.Tests;

public class PriceCheckServiceTests : IDisposable
{
    private const string SkinnoraUrl = "https://skinnora.com/product/nineless-serum";
    private const string KireiUrl = "https://kireibd.com/product/nineless-serum";

    private readonly TestDatabase _database = new();
    private readonly FakeScraper _scraper = new();
    private readonly RecordingNotifier _notifier = new();

    public void Dispose() => _database.Dispose();

    private PriceCheckService Service() => new(_database.CreateContext(), _scraper, _notifier, NullLogger<PriceCheckService>.Instance);

    private async Task<WishlistItemDto> SaveSerumAsync(Guid userId, decimal skinnoraPrice = 1200, decimal kireiPrice = 1500)
    {
        var wishlist = new WishlistService(_database.CreateContext(), new FakeCurrentUser { UserId = userId });
        return await wishlist.AddAsync(new AddWishlistItemRequestDto("nineless|30ml", "Nineless B-Boost Serum 30ml", null,
        [
            new("skinnora.com", skinnoraPrice, SkinnoraUrl),
            new("kireibd.com", kireiPrice, KireiUrl)
        ]), default);
    }

    private void StoreNowSells(string host, string url, decimal price) =>
        _scraper.ByHost[host] = [new ProductResultDto("Nineless B-Boost Serum 30ml", price, url, "", host)];

    [Fact]
    public async Task ADropIsRecordedInTheHistoryAlertedAndNotifiedOnce()
    {
        var user = _database.AddUser();
        await SaveSerumAsync(user.Id);
        StoreNowSells("skinnora.com", SkinnoraUrl, 1000);
        StoreNowSells("kireibd.com", KireiUrl, 1400);

        var summary = await Service().RunAsync(default);

        Assert.Equal(new PriceCheckSummary(1, 2, 2, 2, 1), summary);
        using var context = _database.CreateContext();
        var alerts = await context.PriceAlerts.ToListAsync();
        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a.StoreKey == "skinnora.com" && a.OldPrice == 1200 && a.NewPrice == 1000);
        var offer = await context.WishlistOffers.Include(o => o.History).SingleAsync(o => o.StoreKey == "skinnora.com");
        Assert.Equal(1000, offer.CurrentPrice);
        Assert.Equal(1200, offer.SavedPrice);
        Assert.Equal([1200m, 1000m], offer.History.OrderBy(h => h.RecordedAt).Select(h => h.Price));
        var (notifiedUser, drops) = Assert.Single(_notifier.Sent);
        Assert.Equal(user.Id, notifiedUser);
        Assert.Equal(2, drops.Count);
    }

    [Fact]
    public async Task AnUnchangedPriceCreatesNothingAndARepeatRunDoesNotAlertAgain()
    {
        var user = _database.AddUser();
        await SaveSerumAsync(user.Id);
        StoreNowSells("skinnora.com", SkinnoraUrl, 1200);
        StoreNowSells("kireibd.com", KireiUrl, 1500);

        var unchanged = await Service().RunAsync(default);
        StoreNowSells("skinnora.com", SkinnoraUrl, 1000);
        await Service().RunAsync(default);
        var repeat = await Service().RunAsync(default);

        Assert.Equal(0, unchanged.PricesChanged);
        Assert.Equal(0, repeat.Alerts);
        Assert.Single(_notifier.Sent);
        using var context = _database.CreateContext();
        Assert.Single(await context.PriceAlerts.ToListAsync());
    }

    [Fact]
    public async Task RisesAreRecordedButNeverAlertedAndTinyDropsAreIgnored()
    {
        var user = _database.AddUser();
        await SaveSerumAsync(user.Id);
        StoreNowSells("skinnora.com", SkinnoraUrl, 1300);   // +৳100
        StoreNowSells("kireibd.com", KireiUrl, 1495);       // -৳5, under the minimum

        var summary = await Service().RunAsync(default);

        Assert.Equal(2, summary.PricesChanged);
        Assert.Equal(0, summary.Alerts);
        Assert.Empty(_notifier.Sent);
    }

    [Fact]
    public async Task ImplausiblePricesAreTreatedAsScrapingGlitchesAndIgnored()
    {
        var user = _database.AddUser();
        await SaveSerumAsync(user.Id);
        StoreNowSells("skinnora.com", SkinnoraUrl, 60);      // looks like a sachet, not a 95% sale
        StoreNowSells("kireibd.com", KireiUrl, 90000);

        var summary = await Service().RunAsync(default);

        Assert.Equal(0, summary.PricesChanged);
        using var context = _database.CreateContext();
        Assert.All(await context.WishlistOffers.ToListAsync(), offer => Assert.Equal(offer.SavedPrice, offer.CurrentPrice));
    }

    [Fact]
    public async Task AStoreThatDoesNotAnswerOrNoLongerListsTheProductKeepsItsLastPrice()
    {
        var user = _database.AddUser();
        await SaveSerumAsync(user.Id);
        _scraper.ByHost["skinnora.com"] = [new ProductResultDto("Something else", 500, "https://skinnora.com/product/other", "", "skinnora.com")];
        // kireibd.com returns nothing at all

        var summary = await Service().RunAsync(default);

        Assert.Equal(0, summary.OffersChecked);
        Assert.Equal(0, summary.Alerts);
    }

    [Fact]
    public async Task EachUserGetsOneNotificationWithOnlyTheirOwnDrops()
    {
        var alice = _database.AddUser("alice@example.com");
        var bob = _database.AddUser("bob@example.com");
        await SaveSerumAsync(alice.Id);
        await SaveSerumAsync(bob.Id);
        StoreNowSells("skinnora.com", SkinnoraUrl, 1000);

        var summary = await Service().RunAsync(default);

        Assert.Equal(2, summary.UsersNotified);
        Assert.Equal(2, _notifier.Sent.Count);
        Assert.All(_notifier.Sent, sent => Assert.Single(sent.Drops));
        // Both saved the same product, so each store is searched once for everyone.
        Assert.Equal(1, _scraper.Searched.Count(x => x.StartsWith("skinnora.com|")));
    }

    [Fact]
    public async Task WithNothingSavedItDoesNothing()
    {
        var summary = await Service().RunAsync(default);

        Assert.Equal(new PriceCheckSummary(0, 0, 0, 0, 0), summary);
        Assert.Empty(_scraper.Searched);
    }
}
