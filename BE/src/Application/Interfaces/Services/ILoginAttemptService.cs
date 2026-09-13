namespace Backend.Application.Interfaces.Services;

/// <summary>
/// Outcome of registering a failed login attempt.
/// <para>
/// <see cref="CounterAvailable"/> is <c>false</c> when the counter store (Redis)
/// could not be reached. Callers MUST treat this as a protection failure and
/// fail-closed — never as "zero failures" (which would silently disable
/// brute-force protection). See BE 1.4 §16.
/// </para>
/// </summary>
public readonly record struct LoginAttemptOutcome(bool CounterAvailable, int FailureCount)
{
    public static LoginAttemptOutcome Unavailable => new(false, 0);
    public static LoginAttemptOutcome Counted(int count) => new(true, count);
}

/// <summary>
/// Tracks consecutive failed login attempts per <c>username + client IP</c> in
/// Redis (temporary, TTL-bound counter). The persistent lockout state lives in
/// PostgreSQL (<c>scada.users.lockout_until</c>); this service only counts.
/// </summary>
public interface ILoginAttemptService
{
    /// <summary>
    /// Atomically increments the failure counter for (username, ip) and returns
    /// the new count. TTL is set only on the first failure so the window is fixed
    /// (not sliding). Never throws on Redis failure — returns
    /// <see cref="LoginAttemptOutcome.Unavailable"/> instead.
    /// </summary>
    Task<LoginAttemptOutcome> RegisterFailedAttemptAsync(string username, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Deletes the failure counter for (username, ip) after a successful login.</summary>
    Task ResetAsync(string username, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Current failure count for (username, ip) without incrementing; 0 when absent/unavailable.</summary>
    Task<int> GetFailureCountAsync(string username, string? ipAddress, CancellationToken cancellationToken = default);
}
