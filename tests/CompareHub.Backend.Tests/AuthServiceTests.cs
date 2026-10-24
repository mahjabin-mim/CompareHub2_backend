using FluentValidation;
using Microsoft.Extensions.Options;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Domain.Specifications;
using CompareHub.Backend.app.Core.Infrastructure.Auth;
using CompareHub.Backend.app.Core.Modules.Auth.DTOs;
using CompareHub.Backend.app.Core.Modules.Auth.Interfaces;
using CompareHub.Backend.app.Core.Modules.Auth.Services;
using CompareHub.Backend.app.Core.Shared.Contracts;

namespace CompareHub.Backend.Tests;

public class AuthServiceTests
{
    private readonly FakeUsers _users = new();
    private readonly FakeGoogle _google = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var jwt = Options.Create(new JwtOptions { Issuer = "t", Audience = "t", SecretKey = new string('k', 40), AccessTokenMinutes = 5 });
        _service = new AuthService(_users, new TokenService(jwt), _google, new FakeCurrentUser());
    }

    [Fact]
    public async Task Register_NormalisesEmailHashesPasswordAndReturnsToken()
    {
        var result = await _service.RegisterAsync(new RegisterRequestDto("  Mim  ", "  Mim@Example.COM ", "secret-pass"), default);

        Assert.Equal("mim@example.com", result.Email);
        Assert.Equal("Mim", result.FullName);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.StartsWith("$2", _users.Items.Single().PasswordHash);
    }

    [Theory]
    [InlineData("", "a@b.com", "longenough", "name")]
    [InlineData("Mim", "not-an-email", "longenough", "valid email")]
    [InlineData("Mim", "a@b.com", "short", "Password")]
    public async Task Register_RejectsBadInput(string name, string email, string password, string messagePart)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(new RegisterRequestDto(name, email, password), default));
        Assert.Contains(messagePart, ex.Message);
    }

    [Fact]
    public async Task Register_RejectsADuplicateEmailIgnoringCase()
    {
        await _service.RegisterAsync(new RegisterRequestDto("A", "a@b.com", "longenough"), default);

        await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(new RegisterRequestDto("B", "A@B.com", "longenough"), default));
    }

    [Fact]
    public async Task Login_GivesTheSameErrorForAWrongPasswordAnUnknownEmailAndAGoogleOnlyAccount()
    {
        await _service.RegisterAsync(new RegisterRequestDto("A", "a@b.com", "longenough"), default);
        _users.Items.Add(new AppUser { Email = "g@b.com", FullName = "G", GoogleSubject = "sub-1" });

        var messages = new List<string>();
        foreach (var (email, password) in new[] { ("a@b.com", "wrong-pass"), ("nobody@b.com", "longenough"), ("g@b.com", "longenough") })
        {
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.LoginAsync(new LoginRequestDto(email, password), default));
            messages.Add(ex.Message);
        }

        Assert.Single(messages.Distinct());
        Assert.Equal("a@b.com", (await _service.LoginAsync(new LoginRequestDto(" A@B.com ", "longenough"), default)).Email);
    }

    [Fact]
    public async Task Google_CreatesAnAccountWithoutAPasswordThenReusesIt()
    {
        _google.Identity = new GoogleIdentity("sub-1", "new@b.com", "New User");

        var first = await _service.LoginWithGoogleAsync(new GoogleLoginRequestDto("token", "nonce-nonce-nonce-1234"), default);
        var second = await _service.LoginWithGoogleAsync(new GoogleLoginRequestDto("token", "nonce-nonce-nonce-1234"), default);

        Assert.Equal(first.UserId, second.UserId);
        Assert.Single(_users.Items);
        Assert.Null(_users.Items[0].PasswordHash);
        Assert.Equal("New User", first.FullName);
    }

    [Fact]
    public async Task Google_LinksToAnExistingPasswordAccountWithTheSameVerifiedEmail()
    {
        var registered = await _service.RegisterAsync(new RegisterRequestDto("Mim", "mim@b.com", "longenough"), default);
        _google.Identity = new GoogleIdentity("sub-9", "mim@b.com", "Mim G");

        var viaGoogle = await _service.LoginWithGoogleAsync(new GoogleLoginRequestDto("token", "nonce-nonce-nonce-1234"), default);

        Assert.Equal(registered.UserId, viaGoogle.UserId);
        Assert.Equal("sub-9", _users.Items.Single().GoogleSubject);
        Assert.NotNull(_users.Items.Single().PasswordHash);
    }

    [Fact]
    public async Task Google_PassesThroughAVerifierFailure()
    {
        _google.Failure = new ValidationException("Google sign-in failed. Please try again.");

        await Assert.ThrowsAsync<ValidationException>(() => _service.LoginWithGoogleAsync(new GoogleLoginRequestDto("bad", "nonce-nonce-nonce-1234"), default));
        Assert.Empty(_users.Items);
    }

    private sealed class FakeGoogle : IGoogleTokenVerifier
    {
        public GoogleIdentity Identity { get; set; } = new("sub", "x@y.com", "X");
        public ValidationException? Failure { get; set; }
        public bool IsConfigured => true;
        public string? ClientId => "client";

        public Task<GoogleIdentity> VerifyAsync(string idToken, string nonce, CancellationToken cancellationToken) =>
            Failure is null ? Task.FromResult(Identity) : Task.FromException<GoogleIdentity>(Failure);
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public Guid GetUserId() => Guid.Empty;
        public string GetEmail() => string.Empty;
    }

    private sealed class FakeUsers : IRepository<AppUser>
    {
        public List<AppUser> Items { get; } = [];

        public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<AppUser?> FirstOrDefaultAsync(ISpecification<AppUser> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(specification.Criteria!.Compile()));

        public Task<List<AppUser>> ListAsync(ISpecification<AppUser> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(specification.Criteria!.Compile()).ToList());

        public Task AddAsync(AppUser entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(AppUser entity, CancellationToken cancellationToken = default)
        {
            Items.Remove(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
