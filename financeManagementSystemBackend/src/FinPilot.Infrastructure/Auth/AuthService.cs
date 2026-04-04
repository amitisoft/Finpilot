using System.Security.Authentication;
using FinPilot.Application.DTOs.Auth;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Auth;
using FinPilot.Domain.Entities;
using FinPilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinPilot.Infrastructure.Auth;

public sealed class AuthService(
    FinPilotDbContext dbContext,
    ITokenService tokenService,
    PasswordHasher<User> passwordHasher,
    IAuditLogService? auditLogService = null,
    ILogger<AuthService>? logger = null) : IAuthService
{
    public async Task<RegistrationResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var fullName = NormalizeFullName(request.FullName);

        var existingUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            logger?.LogWarning("Registration rejected for duplicate email {Email} from {IpAddress}", normalizedEmail, ipAddress ?? "unknown");
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            FullName = fullName,
            Email = normalizedEmail,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
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
                    user.FullName,
                    requiresLogin = true
                },
                cancellationToken);
        }

        logger?.LogInformation("User {Email} registered successfully from {IpAddress}", user.Email, ipAddress ?? "unknown");

        return new RegistrationResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RequiresLogin = true
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            logger?.LogWarning("Login rejected for email {Email} from {IpAddress}", normalizedEmail, ipAddress ?? "unknown");
            throw new AuthenticationException("Invalid email or password.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            logger?.LogWarning("Login rejected for email {Email} from {IpAddress}", normalizedEmail, ipAddress ?? "unknown");
            throw new AuthenticationException("Invalid email or password.");
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

        logger?.LogInformation("User {Email} logged in successfully from {IpAddress}", user.Email, ipAddress ?? "unknown");
        return BuildAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var token = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (token is null)
        {
            logger?.LogWarning("Refresh token rejected from {IpAddress}: token not found", ipAddress ?? "unknown");
            throw new AuthenticationException("Invalid refresh token.");
        }

        if (token.RevokedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            logger?.LogWarning("Refresh token rejected for user {UserId} from {IpAddress}: revoked or expired", token.UserId, ipAddress ?? "unknown");
            throw new AuthenticationException("Refresh token is expired or revoked.");
        }

        token.RevokedAt = DateTimeOffset.UtcNow;

        var replacementToken = tokenService.CreateRefreshToken(token.UserId, ipAddress);
        dbContext.RefreshTokens.Add(replacementToken);

        if (token.User is null || !token.User.IsActive)
        {
            logger?.LogWarning("Refresh token rejected for user {UserId} from {IpAddress}: user missing or inactive", token.UserId, ipAddress ?? "unknown");
            throw new AuthenticationException("User not found for refresh token.");
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

    private static string NormalizeEmail(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new InvalidOperationException("Email is required.");
        }

        return normalizedEmail;
    }

    private static string NormalizeFullName(string fullName)
    {
        var normalizedFullName = fullName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            throw new InvalidOperationException("Full name is required.");
        }

        return normalizedFullName;
    }
}
