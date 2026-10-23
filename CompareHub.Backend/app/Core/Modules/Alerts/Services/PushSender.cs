using System.Net;
using System.Text.Json;
using WebPush;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;

namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public class PushSender : IPushSender
{
    private readonly VapidDetails? _vapid;
    private readonly ILogger<PushSender> _logger;

    public PushSender(IConfiguration configuration, ILogger<PushSender> logger)
    {
        _logger = logger;
        var publicKey = configuration["Push:PublicKey"];
        var privateKey = configuration["Push:PrivateKey"];
        if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
        {
            _vapid = new VapidDetails(configuration["Push:Subject"] ?? "mailto:admin@example.com", publicKey, privateKey);
        }
    }

    public bool IsConfigured => _vapid is not null;
    public string? PublicKey => _vapid?.PublicKey;

    public async Task<List<string>> SendAsync(
        IEnumerable<UserPushSubscription> subscriptions, string title, string body, string url, CancellationToken cancellationToken)
    {
        var gone = new List<string>();
        if (_vapid is null)
        {
            return gone;
        }

        var payload = JsonSerializer.Serialize(new { title, body, url });
        var client = new WebPushClient();
        foreach (var subscription in subscriptions)
        {
            try
            {
                await client.SendNotificationAsync(
                    new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth), payload, _vapid, cancellationToken);
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                gone.Add(subscription.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push notification failed for subscription {SubscriptionId}.", subscription.Id);
            }
        }

        return gone;
    }
}
