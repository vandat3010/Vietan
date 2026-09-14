using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Application.Common;
using Backend.Application.DTOs.Auth;
using Backend.Application.Interfaces.Services;
using Backend.Application.Options;
using Backend.Domain.Entities.Scada;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Constants;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// SCADA authentication against <c>scada.users</c>. Passwords are Argon2id;
/// refresh/reset tokens are stored as SHA-256 hashes only.
/// Concurrent login slots are acquired in Redis before JWT issuance.
/// </summary>
public class AuthenticationService(
    ApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IScadaTokenService tokenService,
    ICurrentUserService currentUser,
    IConcurrentLicenseService concurrentLicenseService,
    IConcurrentSessionService concurrentSessionService,
    ILoginAttemptService loginAttempts,
    ISystemAuditService systemAudit,
    IOptions<JwtSettings> jwtOptions,
    IOptions<AuthSettings> authOptions,
    IOptions<LoginSecurityOptions> loginOptions,
    IOptions<PasswordPolicyOptions> passwordOptions,
    IHostEnvironment environment,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;
    private readonly AuthSettings _auth = authOptions.Value;
    private readonly LoginSecurityOptions _login = loginOptions.Value;
    private readonly PasswordPolicyOptions _password = passwordOptions.Value;

    // Process-wide decoy Argon2id hash: verified on the missing/disabled-user path
    // so password verification does real work regardless of account existence,
    // reducing the timing side-channel that leaks whether a username exists.
    private static readonly Lock DecoyLock = new();
    private static string? _decoyHash;

    public async Task<Result<AuthTokenResponse>> RegisterAsync(
        RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            return Result<AuthTokenResponse>.Failure("Auth.Validation", "Username is required.");

        if (!IsPasswordValid(request.Password, out var passwordError))
            return Result<AuthTokenResponse>.Failure("Auth.PasswordPolicy", passwordError);

        if (string.IsNullOrWhiteSpace(request.FullName) && string.IsNullOrWhiteSpace(request.DisplayName))
            return Result<AuthTokenResponse>.Failure("Auth.Validation", "Full name is required.");

        var exists = await db.ScadaUsers.AnyAsync(u => u.Username == username, cancellationToken);
        if (exists)
            return Result<AuthTokenResponse>.Failure("Auth.UsernameTaken", "Username is already taken.");

        var now = DateTimeOffset.UtcNow;
        var fullName = (string.IsNullOrWhiteSpace(request.FullName) ? request.DisplayName : request.FullName).Trim();
        var user = new ScadaUser
        {
            Username = username,
            FullName = fullName,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = string.IsNullOrWhiteSpace(_auth.DefaultRegisterRole) ? "Operator" : _auth.DefaultRegisterRole.Trim(),
            IsActive = true,
            PasswordUpdatedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.ScadaUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var tokens = await TryIssueTokensWithSessionAsync(user, ipAddress, cancellationToken);
        if (tokens.IsFailure)
            return tokens;

        return Result<AuthTokenResponse>.Success(tokens.Value!);
    }

    public async Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        const string invalid = "Invalid username or password.";

        var username = request.Username.Trim();
        var now = DateTimeOffset.UtcNow;
        var user = await db.ScadaUsers.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        // (1) Lockout gate — enforced BEFORE password verification so a correct
        //     password cannot bypass an active lock (BE 1.4 §12). Only a real,
        //     matched account carries a persistent lock; unknown usernames fall
        //     through to the counter path so responses stay indistinguishable (§11).
        if (user is not null && LoginLockoutPolicy.IsLockedOut(user.LockoutUntil, now))
        {
            logger.LogWarning(
                "LoginRejectedBecauseLocked UserId={UserId} Ip={Ip} LockoutUntil={LockoutUntil}",
                user.Id, ipAddress, user.LockoutUntil);
            await AuditAuthAsync(AuditActionNames.LoginFailed, username, ipAddress, user.Id, AuditStatus.Failed, "Login rejected: account temporarily locked", cancellationToken);
            return LockedFailure();
        }

        // (2) Verify password. Verify against a decoy hash when the account is
        //     missing/disabled so timing does not leak account existence (§11).
        var passwordOk = user is not null
            && user.IsActive
            && passwordHasher.Verify(request.Password, user.PasswordHash);

        if (user is null || !user.IsActive)
            _ = passwordHasher.Verify(request.Password, GetDecoyHash());

        if (!passwordOk)
            return await HandleFailedLoginAsync(username, ipAddress, user, now, invalid, cancellationToken);

        // (3) Success — clear the temporary Redis counter first, then issue tokens.
        await loginAttempts.ResetAsync(username, ipAddress, cancellationToken);

        var tokens = await TryIssueTokensWithSessionAsync(user!, ipAddress, cancellationToken);
        if (tokens.IsFailure)
            return tokens;

        user!.LastLoginAt = now;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        // Clear any residual persistent failure state on a successful login.
        if (user.FailedLoginCount != 0 || user.LockoutUntil is not null)
        {
            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
        }
        await db.SaveChangesAsync(cancellationToken);

        if (ConcurrentLicenseService.IsAdminRole(user.Role))
            logger.LogInformation("LoginSucceeded (admin) UserId={UserId} Role={Role}", user.Id, user.Role);
        else
            logger.LogInformation("LoginSucceeded UserId={UserId} Role={Role}", user.Id, user.Role);

        await AuditAuthAsync(AuditActionNames.Login, username, ipAddress, user.Id, AuditStatus.Success, "Login success", cancellationToken);

        return Result<AuthTokenResponse>.Success(tokens.Value!);
    }

    // BE 3.1a/T4.1 — Authentication events are owned by the auth layer (not middleware),
    // so business context is preserved. Generic descriptions avoid username enumeration.
    private Task AuditAuthAsync(string action, string username, string? ipAddress, long? userId, AuditStatus status, string description, CancellationToken cancellationToken) =>
        systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = action,
            EventType = AuditEventType.Authentication,
            Status = status,
            UserId = userId,
            UserName = username,
            IpAddress = ipAddress,
            Module = "Auth",
            Description = description
        }, cancellationToken);

    /// <summary>
    /// BE 1.4 — records a failed attempt in Redis and, on reaching the threshold,
    /// persists a monotonic lockout to PostgreSQL for real accounts. Responses are
    /// identical for existent and non-existent usernames (no enumeration).
    /// </summary>
    private async Task<Result<AuthTokenResponse>> HandleFailedLoginAsync(
        string username, string? ipAddress, ScadaUser? user, DateTimeOffset now, string invalid, CancellationToken cancellationToken)
    {
        var outcome = await loginAttempts.RegisterFailedAttemptAsync(username, ipAddress, cancellationToken);

        // Fail-closed (§16): counter store unreachable → reject, never treat as
        // "0 failures". We cannot count, so we do not (and cannot) lock here.
        if (!outcome.CounterAvailable)
        {
            logger.LogWarning("LoginFailed (RedisCounterError, fail-closed) User={User} Ip={Ip}", username, ipAddress);
            await AuditAuthAsync(AuditActionNames.LoginFailed, username, ipAddress, user?.Id, AuditStatus.Failed, invalid, cancellationToken);
            return Result<AuthTokenResponse>.Failure("Auth.InvalidCredentials", invalid);
        }

        var count = outcome.FailureCount;
        logger.LogInformation("LoginFailed User={User} Ip={Ip} FailureCount={Count}", username, ipAddress, count);

        if (LoginLockoutPolicy.ThresholdReached(count, _login.MaxFailed))
        {
            logger.LogWarning(
                "LoginFailedThresholdReached User={User} Ip={Ip} FailureCount={Count} Max={Max}",
                username, ipAddress, count, _login.MaxFailed);

            if (user is not null)
            {
                var wasLocked = LoginLockoutPolicy.IsLockedOut(user.LockoutUntil, now);
                user.LockoutUntil = LoginLockoutPolicy.ComputeLockoutUntil(user.LockoutUntil, now, _login.LockMinutes);
                user.FailedLoginCount = count;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                logger.LogWarning("AccountLocked UserId={UserId} Until={Until}", user.Id, user.LockoutUntil);

                // T4.1 §3 — AccountLocked is the lock TRANSITION event, audited once. If the
                // account was already locked (a later attempt), it's a rejected attempt, not
                // a new lockout → LoginFailed instead (avoids duplicate AccountLocked rows).
                await AuditAuthAsync(
                    wasLocked ? AuditActionNames.LoginFailed : AuditActionNames.AccountLocked,
                    username, ipAddress, user.Id, AuditStatus.Failed,
                    wasLocked ? "Login rejected: account temporarily locked" : "Account locked after too many failed attempts",
                    cancellationToken);
            }
            else
            {
                // No real account to lock (username does not exist) → record the attempt only.
                await AuditAuthAsync(AuditActionNames.LoginFailed, username, ipAddress, null, AuditStatus.Failed, invalid, cancellationToken);
            }

            return LockedFailure();
        }

        await AuditAuthAsync(AuditActionNames.LoginFailed, username, ipAddress, user?.Id, AuditStatus.Failed, invalid, cancellationToken);
        var remaining = LoginLockoutPolicy.RemainingAttempts(count, _login.MaxFailed);
        return Result<AuthTokenResponse>.Failure("Auth.InvalidCredentials", $"{invalid} Remaining attempts: {remaining}.");
    }

    private static Result<AuthTokenResponse> LockedFailure() =>
        Result<AuthTokenResponse>.Failure("Auth.AccountLocked", "Account temporarily locked due to too many failed login attempts. Please try again later.");

    /// <summary>Lazily hashes a throwaway password once per process for timing parity.</summary>
    private string GetDecoyHash()
    {
        if (_decoyHash is not null)
            return _decoyHash;

        lock (DecoyLock)
        {
            _decoyHash ??= passwordHasher.Hash("Decoy!Password#0-not-real");
        }

        return _decoyHash;
    }

    public async Task<Result<AuthTokenResponse>> RefreshAsync(
        string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<AuthTokenResponse>.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");

        var hash = HashToken(refreshToken);
        var stored = await db.ScadaRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
            return Result<AuthTokenResponse>.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");

        var now = DateTimeOffset.UtcNow;

        // Reuse of an already-revoked token → steal/replay. Kill the whole family.
        if (RefreshTokenSecurityPolicy.IsReuseOfRevokedToken(stored.RevokedAt))
            return await HandleRefreshReuseAsync(stored, ipAddress, cancellationToken);

        if (!stored.User.IsActive || RefreshTokenSecurityPolicy.IsExpired(stored.ExpiresAt, now))
            return Result<AuthTokenResponse>.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");

        if (LoginLockoutPolicy.IsLockedOut(stored.User.LockoutUntil, now))
            return Result<AuthTokenResponse>.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");

        var sessionId = stored.SessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            var acquired = await AcquireSessionAsync(stored.User, cancellationToken);
            if (acquired.IsFailure)
                return Result<AuthTokenResponse>.Failure(acquired.ErrorCode, acquired.Errors);
            sessionId = acquired.Value!;
        }
        else
        {
            var alive = await concurrentSessionService.ExtendAsync(sessionId, cancellationToken);
            if (!alive)
            {
                var reacquired = await AcquireSessionAsync(stored.User, cancellationToken, sessionId);
                if (reacquired.IsFailure)
                    return Result<AuthTokenResponse>.Failure(reacquired.ErrorCode, reacquired.Errors);
                sessionId = reacquired.Value!;
            }
        }

        // Atomic rotate: only one concurrent refresh of this row can win.
        var rows = await db.ScadaRefreshTokens
            .Where(t => t.Id == stored.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedByIp, ipAddress)
                .SetProperty(t => t.UpdatedAt, now), cancellationToken);

        if (RefreshTokenSecurityPolicy.LostRotationRace(rows))
            return Result<AuthTokenResponse>.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");

        // Keep the tracked entity in sync so SaveChanges does not undo ExecuteUpdate.
        stored.RevokedAt = now;
        stored.RevokedByIp = ipAddress;
        stored.UpdatedAt = now;

        var response = await IssueTokensAsync(stored.User, sessionId, ipAddress, cancellationToken, replacedToken: stored);
        return Result<AuthTokenResponse>.Success(response);
    }

    /// <summary>
    /// Stolen/replayed refresh token: revoke every refresh token and Redis session
    /// for that user. Response stays generic (no extra signal to the attacker).
    /// </summary>
    private async Task<Result<AuthTokenResponse>> HandleRefreshReuseAsync(
        ScadaRefreshToken stored, string? ipAddress, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "RefreshTokenReuseDetected UserId={UserId} TokenId={TokenId} Ip={Ip}",
            stored.UserId, stored.Id, ipAddress);

        await RevokeAllRefreshTokensAndSessionsAsync(stored.UserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await AuditAuthAsync(
            AuditActionNames.RefreshTokenReuse,
            stored.User.Username,
            ipAddress,
            stored.UserId,
            AuditStatus.Failed,
            "Refresh token reuse detected; sessions revoked",
            cancellationToken);

        return Result<AuthTokenResponse>.Failure("Auth.InvalidRefreshToken", "Invalid refresh token.");
    }

    public async Task<Result> LogoutAsync(
        string refreshToken, string? sessionIdHint, string? ipAddress, CancellationToken cancellationToken = default)
    {
        // Resolve session from server-side sources only (refresh token / JWT claim).
        // Client SessionId is accepted only when it matches a trusted source.
        string? resolvedSessionId = null;
        ScadaRefreshToken? stored = null;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = HashToken(refreshToken);
            stored = await db.ScadaRefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
            if (stored is not null)
            {
                if (!string.IsNullOrWhiteSpace(stored.SessionId))
                    resolvedSessionId = stored.SessionId;

                if (stored.RevokedAt is null)
                {
                    stored.RevokedAt = DateTimeOffset.UtcNow;
                    stored.RevokedByIp = ipAddress;
                    stored.UpdatedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        var jwtSessionId = currentUser.SessionId;
        if (string.IsNullOrWhiteSpace(resolvedSessionId) && !string.IsNullOrWhiteSpace(jwtSessionId))
            resolvedSessionId = jwtSessionId;

        if (!string.IsNullOrWhiteSpace(sessionIdHint)
            && !string.IsNullOrWhiteSpace(resolvedSessionId)
            && !string.Equals(sessionIdHint, resolvedSessionId, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Logout SessionId hint ignored (does not match trusted session). Trusted={Trusted}",
                resolvedSessionId);
        }

        // Hint only when no trusted session yet (e.g. refresh already revoked) and JWT also empty.
        if (string.IsNullOrWhiteSpace(resolvedSessionId) && !string.IsNullOrWhiteSpace(sessionIdHint))
        {
            // Still do not trust alone for identity; release by id only if Redis has that key
            // (Release is idempotent remove-by-id — no counter decrement).
            resolvedSessionId = sessionIdHint.Trim();
            logger.LogInformation("Logout using SessionId hint without refresh/JWT mapping");
        }

        if (!string.IsNullOrWhiteSpace(resolvedSessionId))
        {
            await concurrentSessionService.ReleaseAsync(resolvedSessionId, cancellationToken);
            logger.LogInformation("Logout released concurrent SessionId={SessionId}", resolvedSessionId);
        }
        else
        {
            logger.LogInformation("Logout completed with no concurrent session to release");
        }

        await AuditAuthAsync(
            AuditActionNames.Logout,
            currentUser.Username ?? stored?.User?.Username ?? "anonymous",
            ipAddress,
            stored?.UserId ?? currentUser.OperatorUserId,
            AuditStatus.Success,
            "Logout",
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.OperatorUserId;
        if (userId is null)
            return Result<CurrentUserResponse>.Failure("Auth.Unauthorized", "Authentication is required.");

        var user = await db.ScadaUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user is null)
            return Result<CurrentUserResponse>.Failure("Auth.NotFound", "User was not found.");

        if (!user.IsActive)
            return Result<CurrentUserResponse>.Failure("Auth.Unauthorized", "Account is disabled.");

        return Result<CurrentUserResponse>.Success(new CurrentUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            DisplayName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            Email = user.Email,
            Unit = user.Unit,
            Level = user.Level,
            Department = user.Department,
            Position = user.Position,
            Description = user.Description,
            CreatedBy = user.CreatedBy,
            UpdatedBy = user.UpdatedBy
        });
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.OperatorUserId;
        if (userId is null)
            return Result.Failure("Auth.Unauthorized", "Authentication is required.");

        if (!IsPasswordValid(request.NewPassword, out var passwordError))
            return Result.Failure("Auth.PasswordPolicy", passwordError);

        var user = await db.ScadaUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user is null)
            return Result.Failure("Auth.NotFound", "User was not found.");

        if (!user.IsActive)
            return Result.Failure("Auth.Unauthorized", "Account is disabled.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Auth.InvalidPassword", "Current password is incorrect.");

        var now = DateTimeOffset.UtcNow;
        // Atomic: password hash + clear "must change" + stamp update time, single SaveChanges.
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.PasswordUpdatedAt = now;
        user.UpdatedAt = now;

        await RevokeAllRefreshTokensAndSessionsAsync(user.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // T4.1 §4 — audit success only; never the old/new password, hash or token.
        await AuditAuthAsync(AuditActionNames.PasswordChanged, user.Username, currentUser.IpAddress, user.Id, AuditStatus.Success, "Password changed", cancellationToken);
        return Result.Success();
    }

    public async Task<Result<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        const string generic = "If the account exists, a password reset request has been created.";
        var response = new ForgotPasswordResponse { Message = generic };

        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            return Result<ForgotPasswordResponse>.Success(response);

        var user = await db.ScadaUsers.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<ForgotPasswordResponse>.Success(response);

        var now = DateTimeOffset.UtcNow;
        var previous = await db.ScadaPasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in previous)
        {
            token.UsedAt = now;
            token.UpdatedAt = now;
        }

        var rawToken = tokenService.CreatePasswordResetToken();
        db.ScadaPasswordResetTokens.Add(new ScadaPasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = now.AddMinutes(_auth.PasswordResetTokenExpirationMinutes),
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);

        if (environment.IsDevelopment())
        {
            response.DevelopmentResetToken = rawToken;
            logger.LogInformation("Password reset token issued for user {Username} (Development only)", username);
        }
        else
        {
            logger.LogInformation("Password reset token issued for user {UserId}", user.Id);
        }

        return Result<ForgotPasswordResponse>.Success(response);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure("Auth.InvalidResetToken", "Invalid or expired reset token.");

        if (!IsPasswordValid(request.NewPassword, out var passwordError))
            return Result.Failure("Auth.PasswordPolicy", passwordError);

        var hash = HashToken(request.Token);
        var stored = await db.ScadaPasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || !stored.IsUsable || !stored.User.IsActive)
            return Result.Failure("Auth.InvalidResetToken", "Invalid or expired reset token.");

        var now = DateTimeOffset.UtcNow;
        stored.UsedAt = now;
        stored.UpdatedAt = now;

        stored.User.PasswordHash = passwordHasher.Hash(request.NewPassword);
        stored.User.MustChangePassword = false;
        stored.User.PasswordUpdatedAt = now;
        stored.User.UpdatedAt = now;

        await RevokeAllRefreshTokensAndSessionsAsync(stored.UserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // T4.1 §4 — reset is [AllowAnonymous]; actor taken from the reset-token record,
        // not from request/claims. Never audit the token or the new password.
        await AuditAuthAsync(AuditActionNames.PasswordChanged, stored.User.Username, currentUser.IpAddress, stored.UserId, AuditStatus.Success, "Password reset via token", cancellationToken);
        return Result.Success();
    }

    public Task<Result<ClaimsPrincipal>> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var principal = tokenService.ValidateAccessToken(accessToken);
        return Task.FromResult(principal is null
            ? Result<ClaimsPrincipal>.Failure("Auth.InvalidToken", "Access token is invalid or expired.")
            : Result<ClaimsPrincipal>.Success(principal));
    }

    private async Task<Result<AuthTokenResponse>> TryIssueTokensWithSessionAsync(
        ScadaUser user,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var acquired = await AcquireSessionAsync(user, cancellationToken);
        if (acquired.IsFailure)
            return Result<AuthTokenResponse>.Failure(acquired.ErrorCode, acquired.Errors);

        var response = await IssueTokensAsync(user, acquired.Value!, ipAddress, cancellationToken);
        return Result<AuthTokenResponse>.Success(response);
    }

    private async Task<Result<string>> AcquireSessionAsync(
        ScadaUser user,
        CancellationToken cancellationToken,
        string? preferredSessionId = null)
    {
        var limit = await concurrentLicenseService.GetEffectiveLimitAsync(cancellationToken);
        var sessionId = string.IsNullOrWhiteSpace(preferredSessionId)
            ? Guid.NewGuid().ToString("N")
            : preferredSessionId;
        var isAdmin = ConcurrentLicenseService.IsAdminRole(user.Role);

        var acquire = await concurrentSessionService.TryAcquireAsync(new ConcurrentSessionAcquireRequest
        {
            SessionId = sessionId,
            UserId = user.Id,
            Role = user.Role,
            IsAdmin = isAdmin,
            MaxConcurrentUsers = limit.MaxConcurrentUsers,
            ReservedAdminSlots = limit.ReservedAdminSlots
        }, cancellationToken);

        if (acquire.Success)
            return Result<string>.Success(sessionId);

        if (acquire.Code == "UNAVAILABLE")
        {
            logger.LogError("Concurrent session acquire failed (Redis). UserId={UserId}", user.Id);
            return Result<string>.Failure("Auth.ConcurrentUnavailable", acquire.Message ?? ConcurrentSessionMessages.RedisUnavailable);
        }

        logger.LogWarning(
            "Concurrent login rejected. UserId={UserId} Role={Role} Max={Max} Admin={Admin} Normal={Normal}",
            user.Id, user.Role, limit.MaxConcurrentUsers, acquire.ActiveAdminUsers, acquire.ActiveNormalUsers);

        return Result<string>.Failure("Auth.ConcurrentLimit", acquire.Message ?? ConcurrentSessionMessages.LimitReached);
    }

    private async Task<AuthTokenResponse> IssueTokensAsync(
        ScadaUser user,
        string sessionId,
        string? ipAddress,
        CancellationToken cancellationToken,
        ScadaRefreshToken? replacedToken = null)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, sessionId);
        var refreshPlain = tokenService.CreateRefreshToken();
        var now = DateTimeOffset.UtcNow;

        var refreshEntity = new ScadaRefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshPlain),
            ExpiresAt = now.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress,
            CreatedAt = now,
            UpdatedAt = now,
            SessionId = sessionId
        };

        db.ScadaRefreshTokens.Add(refreshEntity);
        await db.SaveChangesAsync(cancellationToken);

        if (replacedToken is not null)
        {
            replacedToken.ReplacedByTokenId = refreshEntity.Id;
            replacedToken.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        return new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshPlain,
            AccessTokenExpiresAt = expiresAt,
            SessionId = sessionId,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            DisplayName = user.FullName,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword,
            Email = user.Email,
            Unit = user.Unit,
            Level = user.Level,
            Department = user.Department,
            Position = user.Position,
            Description = user.Description
        };
    }

    private async Task RevokeAllRefreshTokensAndSessionsAsync(long userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await db.ScadaRefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAt = now;
            token.UpdatedAt = now;
            if (!string.IsNullOrWhiteSpace(token.SessionId))
                await concurrentSessionService.ReleaseAsync(token.SessionId, cancellationToken);
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private bool IsPasswordValid(string password, out string error) =>
        PasswordComplexity.TryValidate(
            password,
            _password.MinLength > 0 ? _password.MinLength : PasswordComplexity.DefaultMinLength,
            _password.MaxLength > 0 ? _password.MaxLength : PasswordComplexity.DefaultMaxLength,
            _password.RequireUppercase,
            _password.RequireLowercase,
            _password.RequireDigit,
            _password.RequireSpecial,
            out error);
}
