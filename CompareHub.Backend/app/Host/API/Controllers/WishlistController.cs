using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CompareHub.Backend.app.Core.Modules.Wishlist.DTOs;
using CompareHub.Backend.app.Core.Modules.Wishlist.Interfaces;
using CompareHub.Backend.app.Core.Shared.Common;

namespace CompareHub.Backend.app.Host.API.Controllers;

// Wishlists and price alerts belong to signed-in users only.
[ApiController]
[Authorize]
[Route("api/v1/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(ApiResponse<List<WishlistItemDto>>.Ok(await _wishlistService.ListAsync(cancellationToken)));

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddWishlistItemRequestDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WishlistItemDto>.Ok(await _wishlistService.AddAsync(request, cancellationToken), "Saved to wishlist."));

    // Moves a wishlist that was kept on the device into the account after signing in.
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportWishlistRequestDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<List<WishlistItemDto>>.Ok(await _wishlistService.ImportAsync(request, cancellationToken)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _wishlistService.RemoveAsync(id, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Removed", "Removed from wishlist."));
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts(CancellationToken cancellationToken) =>
        Ok(ApiResponse<AlertsResponseDto>.Ok(await _wishlistService.GetAlertsAsync(cancellationToken)));

    [HttpPost("alerts/read")]
    public async Task<IActionResult> MarkAlertsRead(CancellationToken cancellationToken)
    {
        await _wishlistService.MarkAlertsReadAsync(cancellationToken);
        return Ok(ApiResponse<string>.Ok("Read"));
    }
}
