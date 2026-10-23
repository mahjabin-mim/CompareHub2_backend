using CompareHub.Backend.app.Core.Modules.Wishlist.DTOs;

namespace CompareHub.Backend.app.Core.Modules.Wishlist.Interfaces;

public interface IWishlistService
{
    Task<List<WishlistItemDto>> ListAsync(CancellationToken cancellationToken);
    Task<WishlistItemDto> AddAsync(AddWishlistItemRequestDto request, CancellationToken cancellationToken);
    Task<List<WishlistItemDto>> ImportAsync(ImportWishlistRequestDto request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertsResponseDto> GetAlertsAsync(CancellationToken cancellationToken);
    Task MarkAlertsReadAsync(CancellationToken cancellationToken);
}
