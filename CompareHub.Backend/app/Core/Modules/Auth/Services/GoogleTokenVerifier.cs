using System.Text.Json;
using FluentValidation;
using CompareHub.Backend.app.Core.Modules.Auth.Interfaces;

namespace CompareHub.Backend.app.Core.Modules.Auth.Services;

public class GoogleTokenVerifier : IGoogleTokenVerifier
{
    public const string HttpClientName = "GoogleTokenInfo";
    private static readonly string[] ValidIssuers = ["accounts.google.com", "https://accounts.google.com"];

    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleTokenVerifier(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        ClientId = configuration["Google:ClientId"];
    }

    public string? ClientId { get; }
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);

    public async Task<GoogleIdentity> VerifyAsync(string idToken, string nonce, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new ValidationException("Google sign-in is not set up yet.");
        }

        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 4096 || string.IsNullOrWhiteSpace(nonce) || nonce.Length is < 16 or > 200)
        {
            throw new ValidationException("Google sign-in failed. Please try again.");
        }

        // Google validates the signature and expiry; we still check who the token was issued for.
        using var response = await _httpClientFactory.CreateClient(HttpClientName)
            .GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ValidationException("Google sign-in failed. Please try again.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var claims = document.RootElement;
        string Claim(string name) => claims.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;

        var issuedForUs = Claim("aud") == ClientId;
        // The browser chose the nonce before leaving for Google; a token that carries a different one was
        // not requested by this sign-in attempt (for example a token captured from someone else's login).
        var requestedByThisSignIn = Claim("nonce") == nonce;
        var validIssuer = ValidIssuers.Contains(Claim("iss"));
        var emailVerified = Claim("email_verified").Equals("true", StringComparison.OrdinalIgnoreCase);
        var email = Claim("email");
        var subject = Claim("sub");
        if (!issuedForUs || !requestedByThisSignIn || !validIssuer || !emailVerified || email.Length == 0 || subject.Length == 0)
        {
            throw new ValidationException("Google sign-in failed. Please try again.");
        }

        var name = Claim("name");
        return new GoogleIdentity(subject, email.Trim().ToLowerInvariant(), name.Length > 0 ? name : email.Split('@')[0]);
    }
}
