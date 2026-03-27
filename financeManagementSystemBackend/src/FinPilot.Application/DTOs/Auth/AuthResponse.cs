namespace FinPilot.Application.DTOs.Auth;

public sealed class AuthResponse
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
}
