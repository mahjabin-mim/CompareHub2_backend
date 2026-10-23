namespace CompareHub.Backend.app.Core.Domain.Entities;

public class PriceAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WishlistItemId { get; set; }
    public Guid WishlistOfferId { get; set; }
    public string StoreKey { get; set; } = string.Empty;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    public WishlistItem Item { get; set; } = null!;
}
