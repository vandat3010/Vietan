using System.Net;
using Backend.Application.Interfaces.Services;
using Backend.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// BE 1.4 — Redis-backed failed-login counter, reusing the existing
/// <see cref="IConnectionMultiplexer"/> (no new connection). The counter is a
/// TEMPORARY signal only; the persistent lockout lives in PostgreSQL.
/// <list type="bullet">
/// <item>Key: <c>{prefix}:{username}:{ip}</c> (username normalized like auth).</item>
/// <item>Atomic: single Lua <c>INCR</c> + <c>EXPIRE</c>-when-first — no GET/SET race.</item>
/// <item>Fixed window: TTL set only when the counter is created, never reset.</item>
/// <item>Fail-closed: Redis errors return <see cref="LoginAttemptOutcome.Unavailable"/>, never "0 failures".</item>
/// </list>
/// </summary>
public sealed class RedisLoginAttemptService(
    IConnectionMultiplexer redis,
    IOptions<LoginSecurityOptions> options,
    ILogger<RedisLoginAttemptService> logger) : ILoginAttemptService
{
    // INCR then, only on the very first increment (result == 1), set the TTL.
    // This guarantees a FIXED window: later failures within the window do not
    // extend it. Whole operation is atomic on the Redis server.
    private const string IncrementLua = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
          redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        return current
        """;

    public async Task<LoginAttemptOutcome> RegisterFailedAttemptAsync(
        string username, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(options.Value.RedisKeyPrefix, username, ipAddress);
        var windowSeconds = Math.Max(1, options.Value.WindowMinutes) * 60;

        try
        {
            var db = redis.GetDatabase();
            var result = await db.ScriptEvaluateAsync(IncrementLua, [(RedisKey)key], [windowSeconds]);
            var count = (int)result;
            return LoginAttemptOutcome.Counted(count);
        }
        catch (Exception ex)
        {
            // BE 1.4 §16: do NOT swallow → return 0 → allow. Log a security event
            // and signal unavailability so the caller fails-closed.
            logger.LogError(ex, "RedisCounterError: failed to increment login-failure counter for user {User}", username);
            return LoginAttemptOutcome.Unavailable;
        }
    }

    public async Task ResetAsync(string username, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(options.Value.RedisKeyPrefix, username, ipAddress);
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            // Non-fatal: a stale counter simply expires via its TTL.
            logger.LogWarning(ex, "RedisCounterError: failed to reset login-failure counter for user {User}", username);
        }
    }

    public async Task<int> GetFailureCountAsync(string username, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(options.Value.RedisKeyPrefix, username, ipAddress);
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.HasValue && value.TryParse(out int count) ? count : 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RedisCounterError: failed to read login-failure counter for user {User}", username);
            return 0;
        }
    }

    /// <summary>
    /// Builds the Redis key. Username is trimmed to match the authentication
    /// lookup (case-sensitive, as <c>ScadaUser.Username</c> is compared). IP is
    /// canonicalized (IPv4-mapped IPv6 collapsed) so the same client maps to the
    /// same counter; unknown IPs get a stable placeholder.
    /// </summary>
    public static string BuildKey(string prefix, string username, string? ipAddress)
    {
        var normalizedUser = (username ?? string.Empty).Trim();
        var normalizedIp = CanonicalizeIp(ipAddress);
        return $"{prefix}:{normalizedUser}:{normalizedIp}";
    }

    private static string CanonicalizeIp(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return "unknown";

        if (IPAddress.TryParse(ipAddress, out var parsed))
        {
            if (parsed.IsIPv4MappedToIPv6)
                parsed = parsed.MapToIPv4();
            return parsed.ToString();
        }

        return ipAddress.Trim();
    }
}
