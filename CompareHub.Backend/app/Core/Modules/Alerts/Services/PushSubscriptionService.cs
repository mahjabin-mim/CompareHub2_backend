using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Infrastructure.Auth;
using CompareHub.Backend.app.Core.Infrastructure.Persistence;

namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public record PushSubscriptionRequestDto(string Endpoint, string P256dh, string Auth);
public record PushUnsubscribeRequestDto(string Endpoint);

public class PushSubscriptionService
{
    private const int MaxSubscriptionsPerUser = 10;

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PushSubscriptionService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task SubscribeAsync(PushSubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps ||
            request.Endpoint.Length > 1024 || string.IsNullOrWhiteSpace(request.P256dh) || string.IsNullOrWhiteSpace(request.Auth) ||
            request.P256dh.Length > 256 || request.Auth.Length > 128)
        {
            throw new ValidationException("Invalid notification subscription.");
        }

        var userId = _currentUser.GetUserId();
        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(x => x.Endpoint == request.Endpoint, cancellationToken);
        if (existing is null)
        {
            if (await _db.PushSubscriptions.CountAsync(x => x.UserId == userId, cancellationToken) >= MaxSubscriptionsPerUser)
            {
                throw new ValidationException("Too many devices are subscribed. Turn notifications off on one first.");
            }

            _db.PushSubscriptions.Add(new UserPushSubscription { UserId = userId, Endpoint = request.Endpoint, P256dh = request.P256dh, Auth = request.Auth });
        }
        else
        {
            // The same browser signed in as someone else takes over the subscription.
            existing.UserId = userId;
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnsubscribeAsync(PushUnsubscribeRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        await _db.PushSubscriptions.Where(x => x.UserId == userId && x.Endpoint == request.Endpoint).ExecuteDeleteAsync(cancellationToken);
    }
}
