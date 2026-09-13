using Backend.Domain.Common;

namespace Backend.Domain.Entities;

/// <summary>
/// A rotating refresh token belonging to a <see cref="User"/>. Kept as a child
/// entity of the User aggregate (not its own aggregate root) because a refresh
/// token has no meaning or lifecycle outside of the user it belongs to.
/// </summary>
public class RefreshToken : BaseEntity
{
    private RefreshToken() { } // EF Core

    public RefreshToken(Guid userId, string token, DateTime expiresAtUtc, string? createdByIp)
    {
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        CreatedDate = DateTime.UtcNow;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public string Token { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }

    /// <remarks>Uses the inherited <see cref="BaseEntity{TId}.CreatedDate"/> rather than a redundant field of its own.</remarks>
    public string? CreatedByIp { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? revokedByIp, string? replacedByToken = null)
    {
        RevokedAtUtc = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
    }
}
