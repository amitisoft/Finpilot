using FinPilot.Application.DTOs.Auth;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Auth;
using FinPilot.Domain.Entities;
using FinPilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Auth;

public sealed class AuthService(
    FinPilotDbContext dbContext,
    ITokenService tokenService,
    PasswordHasher<User> passwordHasher,
    IAuditLogService? auditLogService = null) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);

        var refreshToken = tokenService.CreateRefreshToken(user.Id, ipAddress);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(
                user.Id,
                "Auth",
                user.Id,
                "register",
                null,
                new
                {
                    user.Email,
                    user.FullName
                },
                cancellationToken);
        }

        return BuildAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken)
            ?? throw new InvalidOperationException("Invalid email or password.");

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        var refreshToken = await IssueRefreshTokenAsync(user, ipAddress, cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(
                user.Id,
                "Auth",
                user.Id,
                "login",
                null,
                new
                {
                    user.Email
                },
                cancellationToken);
        }

        return BuildAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var token = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken)
            ?? throw new InvalidOperationException("Invalid refresh token.");

        if (token.RevokedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Refresh token is expired or revoked.");
        }

        token.RevokedAt = DateTimeOffset.UtcNow;

        var replacementToken = tokenService.CreateRefreshToken(token.UserId, ipAddress);
        dbContext.RefreshTokens.Add(replacementToken);

        if (token.User is null)
        {
            throw new InvalidOperationException("User not found for refresh token.");
        }

        token.User.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(
                token.UserId,
                "Auth",
                token.UserId,
                "refresh",
                new
                {
                    refreshTokenRotated = true
                },
                new
                {
                    token.User.Email
                },
                cancellationToken);
        }

        return BuildAuthResponse(token.User, replacementToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (token is null)
        {
            return;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(
                token.UserId,
                "Auth",
                token.UserId,
                "logout",
                new
                {
                    tokenIssuedAt = token.CreatedAt
                },
                new
                {
                    revokedAt = token.RevokedAt
                },
                cancellationToken);
        }
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId && x.IsActive)
            .Select(x => new CurrentUserResponse
            {
                UserId = x.Id,
                FullName = x.FullName,
                Email = x.Email
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<RefreshToken> IssueRefreshTokenAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var refreshToken = tokenService.CreateRefreshToken(user.Id, ipAddress);
        dbContext.RefreshTokens.Add(refreshToken);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }

    private AuthResponse BuildAuthResponse(User user, RefreshToken refreshToken)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            AccessToken = tokenService.GenerateAccessToken(user),
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = tokenService.GetAccessTokenExpiry(),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };
    }
}
