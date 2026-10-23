using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;
using CompareHub.Backend.app.Core.Modules.Alerts.Services;
using CompareHub.Backend.app.Core.Shared.Common;

namespace CompareHub.Backend.app.Host.API.Controllers;

[ApiController]
[Route("api/v1/push")]
public class PushController : ControllerBase
{
    private readonly IPushSender _pushSender;
    private readonly PushSubscriptionService _subscriptions;

    public PushController(IPushSender pushSender, PushSubscriptionService subscriptions)
    {
        _pushSender = pushSender;
        _subscriptions = subscriptions;
    }

    // The public half of the server's push key; the browser needs it to create a subscription.
    [HttpGet("public-key")]
    public IActionResult PublicKey() => Ok(ApiResponse<string?>.Ok(_pushSender.PublicKey));

    [Authorize]
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        await _subscriptions.SubscribeAsync(request, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Subscribed", "Notifications are on for this device."));
    }

    [Authorize]
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequestDto request, CancellationToken cancellationToken)
    {
        await _subscriptions.UnsubscribeAsync(request, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Unsubscribed"));
    }
}
