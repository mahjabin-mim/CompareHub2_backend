namespace CompareHub.Backend.app.Core.Modules.Auth.DTOs;

public record RegisterRequestDto(string FullName, string Email, string Password);
public record LoginRequestDto(string Email, string Password);
public record GoogleLoginRequestDto(string IdToken);
public record AuthResponseDto(Guid UserId, string Email, string Token, string FullName = "");
public record AuthConfigDto(string? GoogleClientId);
public record AccountSettingsDto(string Email, string FullName, bool EmailAlertsEnabled, bool HasPassword);
public record UpdateAccountSettingsRequestDto(bool EmailAlertsEnabled);
