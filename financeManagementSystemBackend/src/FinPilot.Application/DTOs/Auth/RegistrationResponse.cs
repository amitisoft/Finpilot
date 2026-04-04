namespace FinPilot.Application.DTOs.Auth;

public sealed class RegistrationResponse
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool RequiresLogin { get; init; } = true;
}
