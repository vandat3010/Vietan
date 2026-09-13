namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Single-use password-reset token. Only the SHA-256 hash is persisted.
/// Mapped to <c>scada.password_reset_tokens</c>.
/// </summary>
public class ScadaPasswordResetToken : ScadaEntity
{
    public long UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public ScadaUser User { get; set; } = null!;

    public bool IsUsable => UsedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
