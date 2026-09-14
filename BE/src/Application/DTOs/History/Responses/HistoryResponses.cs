namespace Backend.Application.DTOs.History;

public class HistorySampleDto
{
    public DateTimeOffset Time { get; set; }
    public long TagId { get; set; }
    public double Value { get; set; }
}

public class AlarmHistoryDto
{
    public long Id { get; set; }
    public long? StationId { get; set; }
    public long? PlcId { get; set; }
    public long? DeviceId { get; set; }
    public long? TagId { get; set; }
    public string? DeviceName { get; set; }
    public string? TagName { get; set; }
    public string? Description { get; set; }
    public string? TroubleshootingGuide { get; set; }
    public string? Type { get; set; }
    public bool IsAcknowledged { get; set; }
    public double? DurationSeconds { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class EventLogDto
{
    public long Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Machine { get; set; }
}

public class UserActivityLogDto
{
    public long Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? Module { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? Status { get; set; }
}

public class AlarmCommandRequest
{
    public string? Note { get; set; }
}
