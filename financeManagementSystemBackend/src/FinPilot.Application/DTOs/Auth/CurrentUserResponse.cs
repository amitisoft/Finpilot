namespace FinPilot.Application.DTOs.Auth;

public sealed class CurrentUserResponse
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
