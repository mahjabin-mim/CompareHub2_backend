namespace CompareHub.Backend.app.Core.Modules.Auth.Interfaces;

public record GoogleIdentity(string Subject, string Email, string Name);

public interface IGoogleTokenVerifier
{
    bool IsConfigured { get; }
    string? ClientId { get; }

    // Throws ValidationException when the token is invalid, expired, unverified or issued for another app.
    Task<GoogleIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
