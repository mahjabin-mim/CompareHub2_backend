using Microsoft.EntityFrameworkCore;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Infrastructure.Persistence;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;

namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public class AlertNotifier : IAlertNotifier
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly IPushSender _push;
    private readonly string _wishlistUrl;
    private readonly ILogger<AlertNotifier> _logger;

    public AlertNotifier(AppDbContext db, IEmailSender email, IPushSender push, IConfiguration configuration, ILogger<AlertNotifier> logger)
    {
        _db = db;
        _email = email;
        _push = push;
        _logger = logger;
        _wishlistUrl = $"{(configuration["App:FrontendUrl"] ?? "https://comparehub2.vercel.app").TrimEnd('/')}/wishlist";
    }

    // One email and one push per user per run, however many of their products dropped.
    public async Task NotifyAsync(AppUser user, IReadOnlyList<PriceDropNotice> drops, CancellationToken cancellationToken)
    {
        if (drops.Count == 0)
        {
            return;
        }

        if (!user.EmailAlertsEnabled || !_email.IsConfigured)
        {
            _logger.LogInformation(
                "Email skipped for user {UserId}: alerts enabled = {Enabled}, email configured = {Configured}.",
                user.Id, user.EmailAlertsEnabled, _email.IsConfigured);
        }
        else
        {
            try
            {
                await _email.SendAsync(
                    user.Email, user.FullName, AlertMessages.EmailSubject(drops), AlertMessages.EmailHtml(user.FullName, drops, _wishlistUrl), cancellationToken);
                _logger.LogInformation("Emailed {Count} price drop(s) to user {UserId}.", drops.Count, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not email price-drop alert to user {UserId}.", user.Id);
            }
        }

        if (_push.IsConfigured)
        {
            var subscriptions = await _db.PushSubscriptions.Where(x => x.UserId == user.Id).ToListAsync(cancellationToken);
            if (subscriptions.Count > 0)
            {
                var gone = await _push.SendAsync(subscriptions, AlertMessages.Title(drops), AlertMessages.PushBody(drops), "/wishlist", cancellationToken);
                _logger.LogInformation("Pushed price drop(s) to {Count} device(s) for user {UserId}; {Gone} expired.", subscriptions.Count, user.Id, gone.Count);
                if (gone.Count > 0)
                {
                    _db.PushSubscriptions.RemoveRange(subscriptions.Where(x => gone.Contains(x.Endpoint)));
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
