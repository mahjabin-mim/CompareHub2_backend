using System.Net;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using CompareHub.Backend.app.Core.Modules.Auth.Services;

namespace CompareHub.Backend.Tests;

public class GoogleTokenVerifierTests
{
    private const string ClientId = "my-client.apps.googleusercontent.com";
    private const string Nonce = "n-0123456789abcdef0123";

    private static GoogleTokenVerifier Verifier(string? clientId, HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(clientId is null ? [] : new Dictionary<string, string?> { ["Google:ClientId"] = clientId })
            .Build();
        return new GoogleTokenVerifier(configuration, new StubHttpClientFactory(status, body));
    }

    private static string Claims(string aud = ClientId, string iss = "https://accounts.google.com", string verified = "true", string email = "Mim@Example.com", string nonce = Nonce) =>
        $$"""{"aud":"{{aud}}","iss":"{{iss}}","email_verified":"{{verified}}","email":"{{email}}","sub":"1234","name":"Mim","nonce":"{{nonce}}"}""";

    [Fact]
    public async Task AValidTokenForThisAppYieldsTheIdentityWithALowercasedEmail()
    {
        var identity = await Verifier(ClientId, body: Claims()).VerifyAsync("token", Nonce, default);

        Assert.Equal(new("1234", "mim@example.com", "Mim"), identity);
    }

    [Fact]
    public void ItReportsWhetherGoogleSignInIsConfigured()
    {
        Assert.True(Verifier(ClientId).IsConfigured);
        Assert.False(Verifier(null).IsConfigured);
        Assert.False(Verifier("  ").IsConfigured);
    }

    [Fact]
    public async Task WithoutAClientIdItRefusesToSignAnyoneIn()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Verifier(null, body: Claims()).VerifyAsync("token", Nonce, default));
        Assert.Contains("not set up", ex.Message);
    }

    [Theory]
    [InlineData("other-app.apps.googleusercontent.com", "https://accounts.google.com", "true")] // token made for another app
    [InlineData(ClientId, "https://evil.example.com", "true")]                                  // not issued by Google
    [InlineData(ClientId, "accounts.google.com", "false")]                                      // unverified email
    public async Task ATokenForAnotherAppFromAnotherIssuerOrWithAnUnverifiedEmailIsRejected(string aud, string iss, string verified)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Verifier(ClientId, body: Claims(aud, iss, verified)).VerifyAsync("token", Nonce, default));
    }

    [Theory]
    [InlineData("a-different-nonce-0123456789")]  // a token requested by some other sign-in
    [InlineData("")]
    public async Task ATokenThatDoesNotCarryThisSignInsNonceIsRejected(string tokenNonce)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Verifier(ClientId, body: Claims(nonce: tokenNonce)).VerifyAsync("token", Nonce, default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task ASignInWithoutAProperNonceIsRejectedWithoutCallingGoogle(string nonce)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Verifier(ClientId, body: Claims(nonce: nonce)).VerifyAsync("token", nonce, default));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AnInvalidOrExpiredTokenIsRejected(HttpStatusCode status)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Verifier(ClientId, status).VerifyAsync("token", Nonce, default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnEmptyTokenIsRejectedWithoutCallingGoogle(string token)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Verifier(ClientId, body: Claims()).VerifyAsync(token, Nonce, default));
    }

    private sealed class StubHttpClientFactory(HttpStatusCode status, string body) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(status, body));
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
