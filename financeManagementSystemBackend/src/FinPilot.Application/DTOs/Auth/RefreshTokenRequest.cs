using System.ComponentModel.DataAnnotations;

namespace FinPilot.Application.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required, StringLength(256, MinimumLength = 20)]
    public string RefreshToken { get; init; } = string.Empty;
}
