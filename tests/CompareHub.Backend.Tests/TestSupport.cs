using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Infrastructure.Auth;
using CompareHub.Backend.app.Core.Infrastructure.Persistence;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.DTOs;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.Interfaces;

namespace CompareHub.Backend.Tests;

// A real database engine (in-memory SQLite) so queries, includes and cascades behave like production.
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public AppUser AddUser(string email = "user@example.com", bool emailAlerts = true)
    {
        using var context = CreateContext();
        var user = new AppUser { Email = email, FullName = "Test User", EmailAlertsEnabled = emailAlerts };
        context.AppUsers.Add(user);
        context.SaveChanges();
        return user;
    }

    public void Dispose() => _connection.Dispose();
}

public sealed class FakeCurrentUser : ICurrentUserService
{
    public Guid UserId { get; set; }
    public Guid GetUserId() => UserId;
    public string GetEmail() => string.Empty;
}

// Answers store searches from a fixed table keyed by store host, like a store's search results.
public sealed class FakeScraper : IProductScraperService
{
    public Dictionary<string, List<ProductResultDto>> ByHost { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Searched { get; } = [];

    public Task<List<ProductResultDto>> SearchProductsAsync(string sourceUrl, string query, CancellationToken cancellationToken)
    {
        var host = new Uri(sourceUrl).Host;
        lock (Searched)
        {
            Searched.Add($"{host}|{query}");
        }

        return Task.FromResult(ByHost.GetValueOrDefault(host) ?? []);
    }
}

public sealed class RecordingNotifier : IAlertNotifier
{
    public List<(Guid UserId, IReadOnlyList<PriceDropNotice> Drops)> Sent { get; } = [];

    public Task NotifyAsync(AppUser user, IReadOnlyList<PriceDropNotice> drops, CancellationToken cancellationToken)
    {
        Sent.Add((user.Id, drops));
        return Task.CompletedTask;
    }
}
