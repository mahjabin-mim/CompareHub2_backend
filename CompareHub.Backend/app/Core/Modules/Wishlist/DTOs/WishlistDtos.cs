namespace CompareHub.Backend.app.Core.Modules.Wishlist.DTOs;

public record WishlistOfferInputDto(string StoreKey, decimal Price, string ProductUrl);

public record AddWishlistItemRequestDto(string Key, string Name, List<string>? ImageUrls, List<WishlistOfferInputDto> Offers);

public record ImportWishlistRequestDto(List<AddWishlistItemRequestDto> Items);

public record PricePointDto(decimal Price, DateTime RecordedAt);

public record WishlistOfferDto(
    Guid Id,
    string StoreKey,
    string ProductUrl,
    decimal SavedPrice,
    decimal CurrentPrice,
    DateTime LastSeenAt,
    List<PricePointDto> History);

public record WishlistItemDto(
    Guid Id,
    string Key,
    string Name,
    List<string> ImageUrls,
    DateTime CreatedAt,
    DateTime? LastCheckedAt,
    List<WishlistOfferDto> Offers);

public record PriceAlertDto(
    Guid Id,
    Guid WishlistItemId,
    string ItemName,
    string StoreKey,
    decimal OldPrice,
    decimal NewPrice,
    DateTime CreatedAt,
    bool IsRead);

public record AlertsResponseDto(List<PriceAlertDto> Alerts, int UnreadCount);
