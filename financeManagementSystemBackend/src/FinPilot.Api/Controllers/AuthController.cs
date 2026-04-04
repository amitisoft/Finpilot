using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Auth;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinPilot.Api.Controllers;

public sealed class AuthController(IAuthService authService, ICurrentUserService currentUserService) : BaseApiController
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Success(response, "User registered successfully. Please sign in to continue.");
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Success(response, "Login successful");
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Success(response, "Token refreshed successfully");
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return Success<object>(null, "Logout successful");
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> Me(CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Unauthorized(ApiResponse<CurrentUserResponse>.Fail("Unauthorized"));
        }

        var response = await authService.GetCurrentUserAsync(currentUserService.UserId.Value, cancellationToken);
        if (response is null)
        {
            return NotFound(ApiResponse<CurrentUserResponse>.Fail("User not found"));
        }

        return Success(response, "Current user fetched successfully");
    }
}
