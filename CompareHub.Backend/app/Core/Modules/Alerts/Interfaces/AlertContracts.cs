using CompareHub.Backend.app.Core.Domain.Entities;

namespace CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;

// One price drop, with what a message needs to describe it.
public record PriceDropNotice(string ItemName, string StoreKey, decimal OldPrice, decimal NewPrice, string ProductUrl);

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken);
}

public interface IPushSender
{
    bool IsConfigured { get; }
    string? PublicKey { get; }

    // Returns the endpoints that are gone for good (expired or unsubscribed) so they can be deleted.
    Task<List<string>> SendAsync(IEnumerable<UserPushSubscription> subscriptions, string title, string body, string url, CancellationToken cancellationToken);
}

public interface IAlertNotifier
{
    Task NotifyAsync(AppUser user, IReadOnlyList<PriceDropNotice> drops, CancellationToken cancellationToken);
}

public record PriceCheckSummary(int Items, int OffersChecked, int PricesChanged, int Alerts, int UsersNotified, bool AlreadyRunning = false);

public interface IPriceCheckService
{
    Task<PriceCheckSummary> RunAsync(CancellationToken cancellationToken);
}
