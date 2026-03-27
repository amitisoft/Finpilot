using System.IdentityModel.Tokens.Jwt;
using FinPilot.Domain.Entities;
using FinPilot.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinPilot.UnitTests.Auth;

public sealed class TokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ShouldContainExpectedClaims()
    {
        var service = CreateService();
        var user = new User
        {
            FullName = "Abhishek Shukla",
            Email = "abhishek@example.com"
        };

        var token = service.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Email, jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.Id.ToString(), jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
    }

    [Fact]
    public void CreateRefreshToken_ShouldSetUserAndExpiry()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var refreshToken = service.CreateRefreshToken(userId, "127.0.0.1");

        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal("127.0.0.1", refreshToken.CreatedByIp);
        Assert.True(refreshToken.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.False(string.IsNullOrWhiteSpace(refreshToken.Token));
    }

    private static TokenService CreateService()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "FinPilot.Api",
            Audience = "FinPilot.Client",
            SecretKey = "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY_FOR_LOCAL_DEVELOPMENT_ONLY_12345",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        return new TokenService(options);
    }
}
