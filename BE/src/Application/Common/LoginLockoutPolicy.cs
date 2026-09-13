namespace Backend.Application.Common;

/// <summary>
/// Pure, side-effect-free decisions for failed-login limiting and account
/// lockout (BE 1.4). Kept separate from the Redis counter and the DB write so
/// the security arithmetic is trivially unit-testable and shared by every caller
/// (no duplicated formulas). All time inputs are UTC.
/// </summary>
public static class LoginLockoutPolicy
{
    /// <summary>Attempts left before lockout. Never negative.</summary>
    public static int RemainingAttempts(int failureCount, int maxFailed) =>
        Math.Max(0, maxFailed - failureCount);

    /// <summary>True once the failure count reaches/exceeds the threshold.</summary>
    public static bool ThresholdReached(int failureCount, int maxFailed) =>
        maxFailed > 0 && failureCount >= maxFailed;

    /// <summary>
    /// Monotonic lockout expiry: a new lockout never shortens an existing one
    /// (protects against concurrent requests writing a shorter window).
    /// </summary>
    public static DateTimeOffset ComputeLockoutUntil(DateTimeOffset? current, DateTimeOffset nowUtc, int lockMinutes)
    {
        var candidate = nowUtc.AddMinutes(lockMinutes);
        return current is { } existing && existing > candidate ? existing : candidate;
    }

    /// <summary>True while the account is locked at the given instant.</summary>
    public static bool IsLockedOut(DateTimeOffset? lockoutUntil, DateTimeOffset nowUtc) =>
        lockoutUntil is { } until && until > nowUtc;

    /// <summary>Seconds remaining on the lockout (for a Retry-After header). Never negative.</summary>
    public static int LockoutRetryAfterSeconds(DateTimeOffset? lockoutUntil, DateTimeOffset nowUtc)
    {
        if (lockoutUntil is not { } until || until <= nowUtc)
            return 0;

        return (int)Math.Ceiling((until - nowUtc).TotalSeconds);
    }
}
