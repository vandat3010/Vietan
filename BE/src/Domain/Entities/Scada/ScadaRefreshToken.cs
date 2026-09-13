namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Opaque refresh token for a <see cref="ScadaUser"/>. Only the SHA-256 hash is stored.
/// Mapped to <c>scada.refresh_tokens</c>.
/// </summary>
public class ScadaRefreshToken : ScadaEntity
{
    public long UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    public string? CreatedByIp { get; set; }

    public long? ReplacedByTokenId { get; set; }

    /// <summary>Concurrent session id (GUID) mirrored in JWT + Redis.</summary>
    public string? SessionId { get; set; }

    public ScadaUser User { get; set; } = null!;

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
