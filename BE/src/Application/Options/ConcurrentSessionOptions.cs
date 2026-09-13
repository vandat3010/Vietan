namespace Backend.Application.Options;

/// <summary>Bound from "ConcurrentSession" in appsettings.</summary>
public class ConcurrentSessionOptions
{
    public const string SectionName = "ConcurrentSession";

    /// <summary>Default total concurrent users when no valid license.</summary>
    public int DefaultMaxConcurrentUsers { get; set; } = 10;

    /// <summary>Slots reserved for Admin/SuperAdmin (not usable by normal users).</summary>
    public int ReservedAdminSlots { get; set; } = 1;

    /// <summary>
    /// Redis session TTL in seconds (abandoned sessions after crash / closed browser).
    /// Idle timeout (e.g. 10 minutes for normal users) is handled by FE via Logout — not by BE.
    /// When 0 or negative, TTL falls back to JWT RefreshTokenExpirationDays.
    /// </summary>
    public int SessionTtlSeconds { get; set; }

    /// <summary>Redis key prefix for multi-tenant isolation.</summary>
    public string RedisKeyPrefix { get; set; } = "tln:concurrent";
}
