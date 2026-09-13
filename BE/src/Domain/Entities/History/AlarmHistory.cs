namespace Backend.Domain.Entities.History;

/// <summary>
/// Alarm history (Timescale hypertable on <c>start_time</c>).
/// Metadata ids are denormalized optionally; names are snapshotted for display
/// after metadata changes. No cross-schema EF foreign keys.
/// Mapped to <c>history.alarm_history</c>.
/// </summary>
public class AlarmHistory
{
    public long Id { get; set; }

    public long? StationId { get; set; }

    public long? PlcId { get; set; }

    public long? DeviceId { get; set; }

    public long? TagId { get; set; }

    public long? TagEventConfigId { get; set; }

    public long? EventTypeId { get; set; }

    public long? TriggerTypeId { get; set; }

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
