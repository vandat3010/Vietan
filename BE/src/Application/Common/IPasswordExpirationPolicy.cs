namespace Backend.Application.Common;

/// <summary>
/// Result of evaluating a password's lifetime. Single source of truth shared by
/// login, /me and the background worker so expiration math is never duplicated.
/// </summary>
public sealed record PasswordExpirationStatus
{
    /// <summary>When the password expires; null when <c>PasswordUpdatedAt</c> is unknown.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Whole days remaining (ceiling). 0 when expired. Null when unknown
    /// (legacy user with no <c>PasswordUpdatedAt</c>) so callers don't show a false countdown.
    /// </summary>
    public int? ExpiresInDays { get; init; }

    public bool IsExpired { get; init; }

    /// <summary>True when within the warning window and not yet expired.</summary>
    public bool ShouldWarn { get; init; }
}

/// <summary>
/// Pure password-lifetime calculator (no DB, no clock of its own). Configured by
/// <c>PasswordPolicyOptions</c>. Callers pass the current UTC instant so the
/// logic stays deterministic and unit-testable.
/// </summary>
public interface IPasswordExpirationPolicy
{
    PasswordExpirationStatus Evaluate(DateTimeOffset? passwordUpdatedAt, DateTimeOffset nowUtc);

    /// <summary>Cutoff for a DB-side "expired" filter: passwords updated at/before this are expired.</summary>
    DateTimeOffset ExpirationCutoffUtc(DateTimeOffset nowUtc);
}
