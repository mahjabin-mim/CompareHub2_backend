using Microsoft.EntityFrameworkCore;
using CompareHub.Backend.app.Core.Domain.Entities;

namespace CompareHub.Backend.app.Core.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<UserSourceLink> UserSourceLinks => Set<UserSourceLink>();
    public DbSet<UserProductSource> UserProductSources => Set<UserProductSource>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<WishlistOffer> WishlistOffers => Set<WishlistOffer>();
    public DbSet<PricePoint> PricePoints => Set<PricePoint>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();
    public DbSet<UserPushSubscription> PushSubscriptions => Set<UserPushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
