namespace Backend.Application.Common;

/// <summary>
/// Pure decisions for refresh-token rotation vs reuse. Distinguishes theft
/// (presenting an already-revoked token) from a lost race (two concurrent
/// refreshes of the same still-active token) so we only revoke the token family
/// on actual reuse — not on the loser of an atomic rotate.
/// </summary>
public static class RefreshTokenSecurityPolicy
{
    public static bool IsExpired(DateTimeOffset expiresAt, DateTimeOffset nowUtc) =>
        expiresAt <= nowUtc;

    /// <summary>A previously revoked token presented again is a reuse/theft signal.</summary>
    public static bool IsReuseOfRevokedToken(DateTimeOffset? revokedAt) =>
        revokedAt is not null;

    /// <summary>
    /// After <c>UPDATE ... WHERE revoked_at IS NULL</c>, zero rows means another
    /// request already rotated this token. Not the same as reuse-after-revoke
    /// (the other request won fairly).
    /// </summary>
    public static bool LostRotationRace(int rowsRevoked) => rowsRevoked == 0;
}
