namespace CompareHub.Backend.app.Core.Domain.Entities;

public class WishlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    // The grouping key the frontend computed when the product was saved.
    public string ProductKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageUrlsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastCheckedAt { get; set; }

    public AppUser User { get; set; } = null!;
    public ICollection<WishlistOffer> Offers { get; set; } = new List<WishlistOffer>();
}

// One store's listing of a wishlist product, whose price is rechecked over time.
public class WishlistOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WishlistItemId { get; set; }
    public string StoreKey { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public decimal SavedPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public WishlistItem Item { get; set; } = null!;
    public ICollection<PricePoint> History { get; set; } = new List<PricePoint>();
}

// Recorded when an offer is first saved and each time its price changes.
public class PricePoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WishlistOfferId { get; set; }
    public decimal Price { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public WishlistOffer Offer { get; set; } = null!;
}
