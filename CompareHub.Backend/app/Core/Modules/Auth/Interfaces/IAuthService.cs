using CompareHub.Backend.app.Core.Modules.Auth.DTOs;

namespace CompareHub.Backend.app.Core.Modules.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginRequestDto request, CancellationToken cancellationToken);
    Task<AccountSettingsDto> GetSettingsAsync(CancellationToken cancellationToken);
    Task<AccountSettingsDto> UpdateSettingsAsync(UpdateAccountSettingsRequestDto request, CancellationToken cancellationToken);
}
