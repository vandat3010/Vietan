using Backend.Domain.Common;
using Backend.Domain.Enums;
using Backend.Domain.Events;
using Backend.Domain.ValueObjects;

namespace Backend.Domain.Entities;

/// <summary>
/// The User aggregate root. All mutations to a user's roles, refresh tokens,
/// password, or lifecycle status must go through this class so invariants
/// (e.g. "a locked-out user cannot log in", "email is always valid/unique")
/// are enforced in one place instead of scattered across services.
/// </summary>
public class User : AuditableEntity
{
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User() { } // EF Core

    private User(Email email, FullName fullName, string passwordHash)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        FailedLoginAttempts = 0;
    }

    public Email Email { get; private set; } = default!;
    public FullName FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserStatus Status { get; private set; }
    public string? PhoneNumber { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEndUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static User Create(Email email, FullName fullName, string passwordHash)
    {
        var user = new User(email, fullName, passwordHash);
        user.AddDomainEvent(new UserCreatedEvent(user.Id, email.Value));
        return user;
    }

    public void UpdateProfile(FullName fullName, string? phoneNumber)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");

        PasswordHash = newPasswordHash;
        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new DomainException("User is already active.");

        Status = UserStatus.Active;
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
    }

    public void Deactivate(string? reason = null)
    {
        if (Status == UserStatus.Inactive)
            throw new DomainException("User is already inactive.");

        Status = UserStatus.Inactive;
        AddDomainEvent(new UserDeactivatedEvent(Id, reason));
    }

    public void Suspend() => Status = UserStatus.Suspended;

    public bool IsLockedOut() =>
        Status == UserStatus.Locked && LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

    public void RecordLoginSuccess()
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        if (Status == UserStatus.Locked)
            Status = UserStatus.Active;

        LastLoginAtUtc = DateTime.UtcNow;
    }

    public void RecordLoginFailure()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            Status = UserStatus.Locked;
            LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
            AddDomainEvent(new UserLockedOutEvent(Id));
        }
    }

    public bool CanLogIn() => Status is UserStatus.Active && !IsLockedOut();

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
            return;

        _userRoles.Add(new UserRole(Id, roleId));
    }

    public void RemoveRole(Guid roleId) => _userRoles.RemoveAll(ur => ur.RoleId == roleId);

    public RefreshToken IssueRefreshToken(string token, DateTime expiresAtUtc, string? createdByIp)
    {
        var refreshToken = new RefreshToken(Id, token, expiresAtUtc, createdByIp);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeRefreshToken(string token, string? revokedByIp, string? replacedByToken = null)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token)
            ?? throw new DomainException("Refresh token not found for this user.");

        if (!refreshToken.IsActive)
            throw new DomainException("Refresh token is already revoked or expired.");

        refreshToken.Revoke(revokedByIp, replacedByToken);
    }

    /// <summary>
    /// Kills every session this user has. Used by logout-everywhere, password
    /// changes and password resets: leaving old refresh tokens alive after a
    /// credential change is how a compromised session outlives its own fix.
    /// </summary>
    public int RevokeAllRefreshTokens(string? revokedByIp)
    {
        var activeTokens = _refreshTokens.Where(rt => rt.IsActive).ToList();
        activeTokens.ForEach(rt => rt.Revoke(revokedByIp));
        return activeTokens.Count;
    }
}
