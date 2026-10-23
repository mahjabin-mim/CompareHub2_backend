namespace CompareHub.Backend.app.Core.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Empty for accounts created with Google sign-in, which have no password.
    public string? PasswordHash { get; set; }
    // Google's stable user id ("sub"); lets a Google account sign in even if its email later changes.
    public string? GoogleSubject { get; set; }
    public bool EmailAlertsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserSourceLink> SourceLinks { get; set; } = new List<UserSourceLink>();
    public ICollection<UserProductSource> ProductSources { get; set; } = new List<UserProductSource>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
