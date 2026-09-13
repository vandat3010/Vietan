using System.Security.Claims;
using Backend.Application.DTOs.Auth;
using Backend.Application.Interfaces.Services;
using Backend.Application.Interfaces.UnitOfWork;
using Backend.Domain.Entities;
using Backend.Domain.Interfaces;
using Backend.Domain.ValueObjects;
using Backend.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Backend.Application.Services;

/// <summary>
/// Coordinates registration, login and refresh-token rotation. Delegates all
/// lockout/attempt-tracking rules to the User aggregate itself, and all
/// token cryptography to <see cref="ITokenService"/>.
/// </summary>
public class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ILogger<AuthService> logger) : IAuthService
{
    private const int RefreshTokenDays = 7;

    public async Task<Result<AuthResultDto>> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
            return Result<AuthResultDto>.Failure("Auth.EmailAlreadyExists", $"Email '{request.Email}' is already registered.");

        var email = Email.Create(request.Email);
        var fullName = FullName.Create(request.FirstName, request.LastName);
        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(email, fullName, passwordHash);

        await unitOfWork.Users.CreateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("New user registered: {UserId}", user.Id);

        return await IssueTokensAsync(user, ipAddress: null, cancellationToken);
    }

    public async Task<Result<AuthResultDto>> LoginAsync(LoginDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user?.RecordLoginFailure();
            if (user is not null)
            {
                unitOfWork.Users.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<AuthResultDto>.Failure("Auth.InvalidCredentials", "Invalid email or password.");
        }

        if (user.IsLockedOut())
            return Result<AuthResultDto>.Failure("Auth.AccountLocked", "Account is temporarily locked due to too many failed login attempts.");

        if (!user.CanLogIn())
            return Result<AuthResultDto>.Failure("Auth.AccountNotActive", "Account is not active.");

        user.RecordLoginSuccess();
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userWithRoles = await unitOfWork.Users.GetWithRolesAsync(user.Id, cancellationToken);
        return await IssueTokensAsync(userWithRoles!, ipAddress, cancellationToken);
    }

    public async Task<Result<AuthResultDto>> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (user is null)
            return Result<AuthResultDto>.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");

        var existingToken = user.RefreshTokens.First(rt => rt.Token == refreshToken);
        if (!existingToken.IsActive)
            return Result<AuthResultDto>.Failure("Auth.InvalidRefreshToken", "Refresh token is expired or has been revoked.");

        var newRefreshToken = tokenService.GenerateRefreshToken();
        user.RevokeRefreshToken(refreshToken, ipAddress, newRefreshToken);
        user.IssueRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(RefreshTokenDays), ipAddress);

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, ipAddress, cancellationToken, precomputedRefreshToken: newRefreshToken);
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (user is null)
            return Result.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");

        user.RevokeRefreshToken(refreshToken, ipAddress);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> LogoutAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("Auth.UserNotFound", "User was not found.");

        var revokedCount = user.RevokeAllRefreshTokens(ipAddress);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} logged out, {RevokedCount} refresh token(s) revoked", userId, revokedCount);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("Auth.UserNotFound", "User was not found.");

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        user.RevokeAllRefreshTokens(revokedByIp: null);

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Password reset performed for user {UserId}", request.UserId);

        return Result.Success();
    }

    public Task<Result<ClaimsPrincipal>> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var principal = tokenService.ValidateAccessToken(accessToken);

        return Task.FromResult(principal is null
            ? Result<ClaimsPrincipal>.Failure("Auth.InvalidToken", "Access token is invalid or expired.")
            : Result<ClaimsPrincipal>.Success(principal));
    }

    private async Task<Result<AuthResultDto>> IssueTokensAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken,
        string? precomputedRefreshToken = null)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
            .Distinct()
            .ToList();

        var (accessToken, expiresAtUtc) = tokenService.GenerateAccessToken(user, roles, permissions);

        string refreshToken;
        if (precomputedRefreshToken is not null)
        {
            refreshToken = precomputedRefreshToken;
        }
        else
        {
            refreshToken = tokenService.GenerateRefreshToken();
            user.IssueRefreshToken(refreshToken, DateTime.UtcNow.AddDays(RefreshTokenDays), ipAddress);
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new AuthResultDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Email = user.Email.Value,
            Roles = roles
        };

        return Result<AuthResultDto>.Success(result);
    }
}
