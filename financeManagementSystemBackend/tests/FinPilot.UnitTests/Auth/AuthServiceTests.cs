using FinPilot.Application.DTOs.Auth;
using FinPilot.Domain.Entities;
using FinPilot.Infrastructure.Auth;
using FinPilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinPilot.UnitTests.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ShouldPersistNewRefreshTokenForExistingUser()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<User>();
        var user = new User
        {
            FullName = "Test User",
            Email = "tester@example.com",
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, "Password@123");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, passwordHasher);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "tester@example.com",
            Password = "Password@123"
        }, "127.0.0.1");

        Assert.Equal(user.Id, response.UserId);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Single(dbContext.RefreshTokens.Where(x => x.UserId == user.Id));
    }

    [Fact]
    public async Task RefreshAsync_ShouldRevokeExistingTokenAndIssueReplacement()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<User>();
        var user = new User
        {
            FullName = "Test User",
            Email = "tester@example.com",
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, "Password@123");
        dbContext.Users.Add(user);

        var tokenService = CreateTokenService();
        var refreshToken = tokenService.CreateRefreshToken(user.Id, "127.0.0.1");
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync();

        var service = new AuthService(dbContext, tokenService, passwordHasher);

        var response = await service.RefreshAsync(new RefreshTokenRequest
        {
            RefreshToken = refreshToken.Token
        }, "127.0.0.1");

        var tokens = dbContext.RefreshTokens.Where(x => x.UserId == user.Id).OrderBy(x => x.CreatedAt).ToList();
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.Equal(tokens[1].Token, response.RefreshToken);
        Assert.NotEqual(tokens[0].Token, tokens[1].Token);
    }

    private static AuthService CreateService(FinPilotDbContext dbContext, PasswordHasher<User> passwordHasher)
    {
        return new AuthService(dbContext, CreateTokenService(), passwordHasher);
    }

    private static TokenService CreateTokenService()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "FinPilot.Api",
            Audience = "FinPilot.Client",
            SecretKey = "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY_FOR_LOCAL_DEVELOPMENT_ONLY_12345",
            AccessTokenMinutes = 120,
            RefreshTokenDays = 7
        });

        return new TokenService(options);
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FinPilotDbContext(options);
    }
}
