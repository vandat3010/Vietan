namespace Backend.Application.Options;

/// <summary>
/// Bound from the "Login" section of appsettings. Drives failed-login limiting
/// and account lockout (BE 1.4a–c). No security threshold is hard-coded in
/// services — they read here. Values are validated at startup so an invalid
/// configuration cannot silently disable brute-force protection.
/// </summary>
public class LoginSecurityOptions
{
    public const string SectionName = "Login";

    /// <summary>Consecutive failed attempts (per username + IP) before lockout. Must be &gt; 0.</summary>
    public int MaxFailed { get; set; } = 5;

    /// <summary>Fixed window (minutes) the Redis failure counter lives for. Must be &gt; 0.</summary>
    public int WindowMinutes { get; set; } = 5;

    /// <summary>How long (minutes) an account stays locked once the threshold is hit. Must be &gt; 0.</summary>
    public int LockMinutes { get; set; } = 15;

    /// <summary>Redis key prefix (multi-tenant isolation). Namespaced like the other SCADA Redis keys.</summary>
    public string RedisKeyPrefix { get; set; } = "tln:auth:login-failed";

    // --- ASP.NET Core RateLimiter (T2.4, defense-in-depth on the login endpoint only) ---

    /// <summary>Enable the fixed-window rate limiter on the login endpoint.</summary>
    public bool RateLimitEnabled { get; set; } = true;

    /// <summary>Max login requests per window per client IP. Must be &gt; 0 when enabled.</summary>
    public int RateLimitPermitPerWindow { get; set; } = 10;

    /// <summary>Rate-limit window in seconds. Must be &gt; 0 when enabled.</summary>
    public int RateLimitWindowSeconds { get; set; } = 60;

    /// <summary>Validates the configuration. Returns false with a message on invalid values.</summary>
    public bool Validate(out string error)
    {
        if (MaxFailed <= 0)
        {
            error = "Login:MaxFailed must be greater than 0.";
            return false;
        }

        if (WindowMinutes <= 0)
        {
            error = "Login:WindowMinutes must be greater than 0.";
            return false;
        }

        if (LockMinutes <= 0)
        {
            error = "Login:LockMinutes must be greater than 0.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(RedisKeyPrefix))
        {
            error = "Login:RedisKeyPrefix must not be empty.";
            return false;
        }

        if (RateLimitEnabled && (RateLimitPermitPerWindow <= 0 || RateLimitWindowSeconds <= 0))
        {
            error = "Login:RateLimitPermitPerWindow and Login:RateLimitWindowSeconds must be greater than 0 when RateLimitEnabled.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
