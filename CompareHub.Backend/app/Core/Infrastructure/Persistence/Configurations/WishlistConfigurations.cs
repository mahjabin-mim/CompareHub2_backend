using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CompareHub.Backend.app.Core.Domain.Entities;

namespace CompareHub.Backend.app.Core.Infrastructure.Persistence.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("wishlist_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.ProductKey).HasColumnName("product_key").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
        builder.Property(x => x.ImageUrlsJson).HasColumnName("image_urls_json").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.LastCheckedAt).HasColumnName("last_checked_at");

        builder.HasOne(x => x.User).WithMany(x => x.WishlistItems).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.UserId);
    }
}

public class WishlistOfferConfiguration : IEntityTypeConfiguration<WishlistOffer>
{
    public void Configure(EntityTypeBuilder<WishlistOffer> builder)
    {
        builder.ToTable("wishlist_offers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WishlistItemId).HasColumnName("wishlist_item_id");
        builder.Property(x => x.StoreKey).HasColumnName("store_key").HasMaxLength(120).IsRequired();
        builder.Property(x => x.ProductUrl).HasColumnName("product_url").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.SavedPrice).HasColumnName("saved_price").HasPrecision(12, 2);
        builder.Property(x => x.CurrentPrice).HasColumnName("current_price").HasPrecision(12, 2);
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").IsRequired();

        builder.HasOne(x => x.Item).WithMany(x => x.Offers).HasForeignKey(x => x.WishlistItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.WishlistItemId);
    }
}

public class PricePointConfiguration : IEntityTypeConfiguration<PricePoint>
{
    public void Configure(EntityTypeBuilder<PricePoint> builder)
    {
        builder.ToTable("price_points");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WishlistOfferId).HasColumnName("wishlist_offer_id");
        builder.Property(x => x.Price).HasColumnName("price").HasPrecision(12, 2);
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at").IsRequired();

        builder.HasOne(x => x.Offer).WithMany(x => x.History).HasForeignKey(x => x.WishlistOfferId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.WishlistOfferId, x.RecordedAt });
    }
}

public class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("price_alerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.WishlistItemId).HasColumnName("wishlist_item_id");
        builder.Property(x => x.WishlistOfferId).HasColumnName("wishlist_offer_id");
        builder.Property(x => x.StoreKey).HasColumnName("store_key").HasMaxLength(120).IsRequired();
        builder.Property(x => x.OldPrice).HasColumnName("old_price").HasPrecision(12, 2);
        builder.Property(x => x.NewPrice).HasColumnName("new_price").HasPrecision(12, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ReadAt).HasColumnName("read_at");

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.WishlistItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}

public class UserPushSubscriptionConfiguration : IEntityTypeConfiguration<UserPushSubscription>
{
    public void Configure(EntityTypeBuilder<UserPushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Endpoint).HasColumnName("endpoint").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.P256dh).HasColumnName("p256dh").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Auth).HasColumnName("auth").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Endpoint).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
