using System.Net.Mail;
using FluentValidation;
using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Infrastructure.Auth;
using CompareHub.Backend.app.Core.Modules.Auth.DTOs;
using CompareHub.Backend.app.Core.Modules.Auth.Interfaces;
using CompareHub.Backend.app.Core.Modules.Auth.Specifications;
using CompareHub.Backend.app.Core.Shared.Contracts;

namespace CompareHub.Backend.app.Core.Modules.Auth.Services;

public class AuthService : IAuthService
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 72; // BCrypt ignores everything past 72 bytes.

    // Verified against when the email is unknown, so a missing account takes as long as a wrong password.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("not-a-real-password");

    private readonly IRepository<AppUser> _users;
    private readonly ITokenService _tokenService;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly ICurrentUserService _currentUser;

    public AuthService(
        IRepository<AppUser> users,
        ITokenService tokenService,
        IGoogleTokenVerifier googleTokenVerifier,
        ICurrentUserService currentUser)
    {
        _users = users;
        _tokenService = tokenService;
        _googleTokenVerifier = googleTokenVerifier;
        _currentUser = currentUser;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var fullName = (request.FullName ?? string.Empty).Trim();
        var email = NormalizeEmail(request.Email);
        if (fullName.Length is 0 or > 120)
        {
            throw new ValidationException("Please enter your name.");
        }

        if (!IsValidEmail(email))
        {
            throw new ValidationException("Please enter a valid email address.");
        }

        if ((request.Password ?? string.Empty).Length is < MinPasswordLength or > MaxPasswordLength)
        {
            throw new ValidationException($"Password must be {MinPasswordLength}-{MaxPasswordLength} characters.");
        }

        var existing = await _users.FirstOrDefaultAsync(new AppUserByEmailSpecification(email), cancellationToken);
        if (existing is not null)
        {
            throw new ValidationException("Email is already registered. Try signing in instead.");
        }

        var user = new AppUser
        {
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _users.FirstOrDefaultAsync(new AppUserByEmailSpecification(NormalizeEmail(request.Email)), cancellationToken);
        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, user?.PasswordHash ?? DummyHash);
        if (user is null || user.PasswordHash is null || !passwordOk)
        {
            throw new ValidationException("Invalid email or password.");
        }

        return ToResponse(user);
    }

    public async Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginRequestDto request, CancellationToken cancellationToken)
    {
        var identity = await _googleTokenVerifier.VerifyAsync(request.IdToken, cancellationToken);

        var user = await _users.FirstOrDefaultAsync(new AppUserByGoogleSubjectSpecification(identity.Subject), cancellationToken)
            ?? await _users.FirstOrDefaultAsync(new AppUserByEmailSpecification(identity.Email), cancellationToken);

        if (user is null)
        {
            user = new AppUser { FullName = identity.Name, Email = identity.Email, GoogleSubject = identity.Subject };
            await _users.AddAsync(user, cancellationToken);
        }
        else if (user.GoogleSubject is null)
        {
            // Same verified email as an existing password account: link them instead of creating a duplicate.
            user.GoogleSubject = identity.Subject;
        }

        await _users.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<AccountSettingsDto> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return ToSettings(user);
    }

    public async Task<AccountSettingsDto> UpdateSettingsAsync(UpdateAccountSettingsRequestDto request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        user.EmailAlertsEnabled = request.EmailAlertsEnabled;
        await _users.SaveChangesAsync(cancellationToken);
        return ToSettings(user);
    }

    private async Task<AppUser> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        await _users.GetByIdAsync(_currentUser.GetUserId(), cancellationToken)
        ?? throw new ValidationException("Account not found.");

    private AuthResponseDto ToResponse(AppUser user) => new(user.Id, user.Email, _tokenService.GenerateToken(user), user.FullName);

    private static AccountSettingsDto ToSettings(AppUser user) =>
        new(user.Email, user.FullName, user.EmailAlertsEnabled, user.PasswordHash is not null);

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email) =>
        email.Length is > 3 and <= 160 && MailAddress.TryCreate(email, out var parsed) && parsed.Address == email && email.Contains('.');
}
