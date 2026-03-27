using FinPilot.Domain.Entities;

namespace FinPilot.Application.Interfaces.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken CreateRefreshToken(Guid userId, string? ipAddress);
    DateTimeOffset GetAccessTokenExpiry();
}
