namespace CompareHub.Backend.app.Core.Modules.Auth.Interfaces;

public record GoogleIdentity(string Subject, string Email, string Name);

public interface IGoogleTokenVerifier
{
    bool IsConfigured { get; }
    string? ClientId { get; }

    // Throws ValidationException when the token is invalid, expired, unverified, issued for another app,
    // or was not requested with this nonce (the random value the browser put in the sign-in request).
    Task<GoogleIdentity> VerifyAsync(string idToken, string nonce, CancellationToken cancellationToken);
}
