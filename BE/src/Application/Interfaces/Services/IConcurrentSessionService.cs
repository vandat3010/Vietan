using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

public sealed class ConcurrentLimitInfo
{
    public int MaxConcurrentUsers { get; init; }
    public int ReservedAdminSlots { get; init; }
    public int MaxNormalConcurrentUsers => Math.Max(0, MaxConcurrentUsers - ReservedAdminSlots);
    public Guid? ActiveLicenseId { get; init; }
    public bool UsingLicense { get; init; }
}

public sealed class ConcurrentSessionAcquireRequest
{
    public required string SessionId { get; init; }
    public required long UserId { get; init; }
    public required string Role { get; init; }
    public required bool IsAdmin { get; init; }
    public required int MaxConcurrentUsers { get; init; }
    public required int ReservedAdminSlots { get; init; }
}

public sealed class ConcurrentSessionAcquireResult
{
    public bool Success { get; init; }
    public string Code { get; init; } = string.Empty;
    public int ActiveAdminUsers { get; init; }
    public int ActiveNormalUsers { get; init; }
    public string? Message { get; init; }

    public static ConcurrentSessionAcquireResult Ok(int admin, int normal) =>
        new() { Success = true, Code = "OK", ActiveAdminUsers = admin, ActiveNormalUsers = normal };

    public static ConcurrentSessionAcquireResult LimitReached(int admin, int normal, string message) =>
        new() { Success = false, Code = "LIMIT", ActiveAdminUsers = admin, ActiveNormalUsers = normal, Message = message };

    public static ConcurrentSessionAcquireResult Unavailable(string message) =>
        new() { Success = false, Code = "UNAVAILABLE", Message = message };
}

public sealed class ConcurrentUsersStatusDto
{
    public int MaxConcurrentUsers { get; set; }
    public int ReservedAdminSlots { get; set; }
    public int MaxNormalConcurrentUsers { get; set; }
    public int ActiveAdminUsers { get; set; }
    public int ActiveNormalUsers { get; set; }
    public int TotalActiveUsers { get; set; }
    public int AvailableNormalSlots { get; set; }
    public bool UsingLicense { get; set; }
}

/// <summary>Resolves effective concurrent-user limit from DB license + config defaults.</summary>
public interface IConcurrentLicenseService
{
    Task<ConcurrentLimitInfo> GetEffectiveLimitAsync(CancellationToken cancellationToken = default);
}

/// <summary>Redis-backed atomic concurrent session store.</summary>
public interface IConcurrentSessionService
{
    Task<ConcurrentSessionAcquireResult> TryAcquireAsync(ConcurrentSessionAcquireRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends Redis TTL for an existing session (e.g. on refresh-token rotation).
    /// Not an idle/activity heartbeat — FE idle logout calls Logout instead.
    /// </summary>
    Task<bool> ExtendAsync(string sessionId, CancellationToken cancellationToken = default);

    Task ReleaseAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<Result<ConcurrentUsersStatusDto>> GetStatusAsync(CancellationToken cancellationToken = default);
}
