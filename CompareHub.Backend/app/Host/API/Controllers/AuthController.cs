using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CompareHub.Backend.app.Core.Modules.Auth.DTOs;
using CompareHub.Backend.app.Core.Modules.Auth.Interfaces;
using CompareHub.Backend.app.Core.Shared.Common;

namespace CompareHub.Backend.app.Host.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;

    public AuthController(IAuthService authService, IGoogleTokenVerifier googleTokenVerifier)
    {
        _authService = authService;
        _googleTokenVerifier = googleTokenVerifier;
    }

    // Lets the frontend show the Google button only when the server has a Google client configured.
    [HttpGet("config")]
    public IActionResult GetConfig() =>
        Ok(ApiResponse<AuthConfigDto>.Ok(new AuthConfigDto(_googleTokenVerifier.ClientId)));

    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "User registered successfully."));
    }

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    [EnableRateLimiting("auth")]
    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleLoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginWithGoogleAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    [Authorize]
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken) =>
        Ok(ApiResponse<AccountSettingsDto>.Ok(await _authService.GetSettingsAsync(cancellationToken)));

    [Authorize]
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAccountSettingsRequestDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AccountSettingsDto>.Ok(await _authService.UpdateSettingsAsync(request, cancellationToken)));
}
