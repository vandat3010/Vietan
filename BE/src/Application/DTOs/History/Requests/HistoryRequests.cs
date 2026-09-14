using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.History;

/// <summary>
/// Time-range query for history_1s / history_1m / history_30m.
/// From và To bắt buộc.
/// </summary>
public class HistorySampleQuery : PaginationRequest
{
    public long? TagId { get; set; }

    /// <summary>Khi cả TagId và TagIds có giá trị, TagIds ưu tiên nếu non-empty.</summary>
    public IList<long>? TagIds { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }
}

public class AlarmHistoryQuery : PaginationRequest
{
    public long? TagId { get; set; }
    public long? DeviceId { get; set; }
    public long? StationId { get; set; }
    public string? Type { get; set; }
    public bool? IsAcknowledged { get; set; }

    /// <summary>
    /// When true, only open alarms (<c>EndTime IS NULL</c>).
    /// Independent of <see cref="IsAcknowledged"/>.
    /// </summary>
    public bool? ActiveOnly { get; set; }

    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public class EventLogQuery : PaginationRequest
{
    public string? Level { get; set; }
    public string? Module { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public class UserActivityLogQuery : PaginationRequest
{
    public string? Username { get; set; }
    public string? ActionType { get; set; }
    public string? Module { get; set; }
    public string? Status { get; set; }
    public long? UserId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
