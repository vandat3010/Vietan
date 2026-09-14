using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

/// <summary>Query alarm đang mở (EndTime IS NULL).</summary>
public class ActiveAlarmQuery : PaginationRequest
{
    public long? DeviceId { get; set; }
    public bool? IsAcknowledged { get; set; }
    public string? Type { get; set; }
}

public class ActiveAlarmRowDto
{
    public long Id { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public long? TagId { get; set; }
    public string? TagName { get; set; }
    public bool IsAcknowledged { get; set; }
    public long? StationId { get; set; }
}
