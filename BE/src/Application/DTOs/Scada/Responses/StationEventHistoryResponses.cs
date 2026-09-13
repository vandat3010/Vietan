namespace Backend.Application.DTOs.Scada;

/// <summary>1 dòng lịch sử sự kiện (màn Events / History).</summary>
public class StationEventHistoryRowDto
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
    public string? Username { get; set; }
    public long? EventTypeId { get; set; }
    public bool IsAcknowledged { get; set; }
}
