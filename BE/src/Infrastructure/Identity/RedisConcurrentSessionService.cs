using Backend.Application.Interfaces.Services;
using Backend.Application.Options;
using Backend.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// Atomic concurrent session management via Redis Sorted Sets + Lua.
/// Admin sessions: zset {prefix}:admin ; Normal: {prefix}:normal ;
/// Session hash: {prefix}:session:{sessionId}
/// </summary>
public class RedisConcurrentSessionService(
    IConnectionMultiplexer redis,
    IOptions<ConcurrentSessionOptions> options,
    IOptions<JwtSettings> jwtOptions,
    ILogger<RedisConcurrentSessionService> logger) : IConcurrentSessionService
{
    private const string AcquireLua = """
        local adminKey = KEYS[1]
        local normalKey = KEYS[2]
        local sessionKey = KEYS[3]
        local now = tonumber(ARGV[1])
        local expireAt = tonumber(ARGV[2])
        local sessionId = ARGV[3]
        local userId = ARGV[4]
        local role = ARGV[5]
        local isAdmin = tonumber(ARGV[6])
        local maxTotal = tonumber(ARGV[7])
        local reservedAdmin = tonumber(ARGV[8])
        local maxAdmin = tonumber(ARGV[9])
        local ttl = tonumber(ARGV[10])
        local sessionPrefix = ARGV[11]

        redis.call('ZREMRANGEBYSCORE', adminKey, '-inf', now)
        redis.call('ZREMRANGEBYSCORE', normalKey, '-inf', now)

        local adminCount = redis.call('ZCARD', adminKey)
        local normalCount = redis.call('ZCARD', normalKey)

        if isAdmin == 1 then
          while adminCount >= maxAdmin do
            local oldest = redis.call('ZRANGE', adminKey, 0, 0)
            if (not oldest) or (#oldest == 0) then break end
            local oldId = oldest[1]
            redis.call('ZREM', adminKey, oldId)
            redis.call('DEL', sessionPrefix .. oldId)
            adminCount = adminCount - 1
          end
          redis.call('ZADD', adminKey, expireAt, sessionId)
        else
          local maxNormal = maxTotal - reservedAdmin
          if maxNormal < 0 then maxNormal = 0 end
          if normalCount >= maxNormal then
            return {0, 'LIMIT', adminCount, normalCount}
          end
          redis.call('ZADD', normalKey, expireAt, sessionId)
        end

        redis.call('HSET', sessionKey,
          'userId', userId,
          'role', role,
          'isAdmin', isAdmin,
          'expireAt', expireAt)
        redis.call('EXPIRE', sessionKey, ttl)

        adminCount = redis.call('ZCARD', adminKey)
        normalCount = redis.call('ZCARD', normalKey)
        return {1, 'OK', adminCount, normalCount}
        """;

    private const string ExtendLua = """
        local adminKey = KEYS[1]
        local normalKey = KEYS[2]
        local sessionKey = KEYS[3]
        local sessionId = ARGV[1]
        local now = tonumber(ARGV[2])
        local expireAt = tonumber(ARGV[3])
        local ttl = tonumber(ARGV[4])

        if redis.call('EXISTS', sessionKey) == 0 then
          return 0
        end

        local inAdmin = redis.call('ZSCORE', adminKey, sessionId)
        local inNormal = redis.call('ZSCORE', normalKey, sessionId)
        if (not inAdmin) and (not inNormal) then
          return 0
        end

        if inAdmin then
          redis.call('ZADD', adminKey, expireAt, sessionId)
        end
        if inNormal then
          redis.call('ZADD', normalKey, expireAt, sessionId)
        end
        redis.call('HSET', sessionKey, 'expireAt', expireAt)
        redis.call('EXPIRE', sessionKey, ttl)
        return 1
        """;

    private const string ReleaseLua = """
        local adminKey = KEYS[1]
        local normalKey = KEYS[2]
        local sessionKey = KEYS[3]
        local sessionId = ARGV[1]
        redis.call('ZREM', adminKey, sessionId)
        redis.call('ZREM', normalKey, sessionId)
        redis.call('DEL', sessionKey)
        return 1
        """;

    private int ResolveTtlSeconds()
    {
        var configured = options.Value.SessionTtlSeconds;
        if (configured > 0)
            return configured;

        var days = Math.Max(1, jwtOptions.Value.RefreshTokenExpirationDays);
        return days * 24 * 60 * 60;
    }

    public async Task<ConcurrentSessionAcquireResult> TryAcquireAsync(
        ConcurrentSessionAcquireRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var ttl = ResolveTtlSeconds();
            var expireAt = now + ttl;
            var maxAdmin = Math.Max(1, request.ReservedAdminSlots);

            var keys = BuildKeys(request.SessionId);
            var result = await db.ScriptEvaluateAsync(
                AcquireLua,
                keys,
                [
                    now,
                    expireAt,
                    request.SessionId,
                    request.UserId.ToString(),
                    request.Role,
                    request.IsAdmin ? 1 : 0,
                    request.MaxConcurrentUsers,
                    request.ReservedAdminSlots,
                    maxAdmin,
                    ttl,
                    $"{options.Value.RedisKeyPrefix}:session:"
                ]);

            var arr = (RedisResult[])result!;
            var ok = (int)arr[0] == 1;
            var code = (string)arr[1]!;
            var admin = (int)arr[2];
            var normal = (int)arr[3];

            if (ok)
            {
                logger.LogInformation(
                    "Concurrent session acquired. SessionId={SessionId} UserId={UserId} IsAdmin={IsAdmin} Admin={Admin} Normal={Normal}",
                    request.SessionId, request.UserId, request.IsAdmin, admin, normal);
                return ConcurrentSessionAcquireResult.Ok(admin, normal);
            }

            var message = ConcurrentSessionMessages.LimitReached;
            logger.LogWarning(
                "Concurrent login rejected (limit). UserId={UserId} IsAdmin={IsAdmin} Admin={Admin} Normal={Normal} Max={Max}",
                request.UserId, request.IsAdmin, admin, normal, request.MaxConcurrentUsers);
            return ConcurrentSessionAcquireResult.LimitReached(admin, normal, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis unavailable while acquiring concurrent session for UserId={UserId}", request.UserId);
            return ConcurrentSessionAcquireResult.Unavailable(ConcurrentSessionMessages.RedisUnavailable);
        }
    }

    public async Task<bool> ExtendAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        try
        {
            var db = redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var ttl = ResolveTtlSeconds();
            var expireAt = now + ttl;
            var keys = BuildKeys(sessionId);

            var result = await db.ScriptEvaluateAsync(
                ExtendLua,
                keys,
                [sessionId, now, expireAt, ttl]);

            var ok = (int)result == 1;
            if (ok)
                logger.LogDebug("Concurrent session extended SessionId={SessionId}", sessionId);
            else
                logger.LogInformation("Concurrent session extend failed (missing/expired) SessionId={SessionId}", sessionId);
            return ok;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis error extending SessionId={SessionId}", sessionId);
            return false;
        }
    }

    public async Task ReleaseAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;

        try
        {
            var db = redis.GetDatabase();
            var keys = BuildKeys(sessionId);
            await db.ScriptEvaluateAsync(ReleaseLua, keys, [sessionId]);
            logger.LogInformation("Concurrent session released SessionId={SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis error releasing SessionId={SessionId}", sessionId);
        }
    }

    public async Task<Result<ConcurrentUsersStatusDto>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        try
        {
            var db = redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var prefix = cfg.RedisKeyPrefix;
            var adminKey = $"{prefix}:admin";
            var normalKey = $"{prefix}:normal";

            await db.SortedSetRemoveRangeByScoreAsync(adminKey, double.NegativeInfinity, now);
            await db.SortedSetRemoveRangeByScoreAsync(normalKey, double.NegativeInfinity, now);

            var admin = (int)await db.SortedSetLengthAsync(adminKey);
            var normal = (int)await db.SortedSetLengthAsync(normalKey);
            var max = cfg.DefaultMaxConcurrentUsers;
            var reserved = cfg.ReservedAdminSlots;
            var maxNormal = Math.Max(0, max - reserved);

            return Result<ConcurrentUsersStatusDto>.Success(new ConcurrentUsersStatusDto
            {
                MaxConcurrentUsers = max,
                ReservedAdminSlots = reserved,
                MaxNormalConcurrentUsers = maxNormal,
                ActiveAdminUsers = admin,
                ActiveNormalUsers = normal,
                TotalActiveUsers = admin + normal,
                AvailableNormalSlots = Math.Max(0, maxNormal - normal),
                UsingLicense = false
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis error reading concurrent status");
            return Result<ConcurrentUsersStatusDto>.Failure(
                "Auth.ConcurrentUnavailable",
                ConcurrentSessionMessages.RedisUnavailable);
        }
    }

    private RedisKey[] BuildKeys(string sessionId)
    {
        var prefix = options.Value.RedisKeyPrefix;
        return
        [
            $"{prefix}:admin",
            $"{prefix}:normal",
            $"{prefix}:session:{sessionId}"
        ];
    }
}

public static class ConcurrentSessionMessages
{
    public const string LimitReached =
        "Đã đạt giới hạn số lượng người dùng đăng nhập đồng thời. Vui lòng đăng xuất một tài khoản khác hoặc sử dụng License Key để mở rộng giới hạn.";

    public const string LicenseInvalid =
        "License Key không hợp lệ hoặc đã hết hạn.";

    public const string RedisUnavailable =
        "Không thể kiểm tra giới hạn đăng nhập đồng thời. Vui lòng thử lại sau.";
}
