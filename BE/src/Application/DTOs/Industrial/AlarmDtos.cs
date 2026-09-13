using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Industrial;

/// <summary>
/// An alarm occurrence. Acknowledge and clear are modelled as separate nullable
/// timestamps rather than a single "closed" flag because operators are audited on
/// both moments independently: an alarm can be acknowledged while still active,
/// and can clear itself without ever being acknowledged.
/// </summary>
public class AlarmDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }

    /// <summary>Null for device-level alarms that are not attributable to one tag.</summary>
    public Guid? TagId { get; set; }

    /// <summary>Stable machine-readable key so clients can localise or route on the alarm without parsing <see cref="Message"/>.</summary>
    public string Code { get; set; } = default!;

    public string Message { get; set; } = default!;
    public AlarmSeverity Severity { get; set; }
    public AlarmState State { get; set; }
    public DateTime RaisedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? ClearedAtUtc { get; set; }
}

/// <summary>
/// Raise request. Deliberately excludes the lifecycle fields of
/// <see cref="AlarmDto"/> so a caller cannot fabricate an acknowledgement.
/// </summary>
public class CreateAlarmDto
{
    public Guid DeviceId { get; set; }
    public Guid? TagId { get; set; }
    public string Code { get; set; } = default!;
    public string Message { get; set; } = default!;
    public AlarmSeverity Severity { get; set; }

    /// <summary>Null lets the implementation stamp the server clock; supplied when a device or gateway replays a buffered alarm.</summary>
    public DateTime? RaisedAtUtc { get; set; }
}

/// <summary>
/// Alarm history is unbounded, so it inherits <see cref="PaginationRequest"/>
/// rather than defining its own page fields - the time window and filters below
/// narrow the set, paging bounds the response.
/// </summary>
public class AlarmHistoryQuery : PaginationRequest
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    /// <summary>Null means all devices.</summary>
    public Guid? DeviceId { get; set; }

    /// <summary>Null means all severities; set to focus an incident review on the significant ones.</summary>
    public AlarmSeverity? Severity { get; set; }
}

/// <summary>
/// Ordered from least to most urgent so callers can filter with a simple
/// comparison (<c>Severity &gt;= Major</c>) instead of an explicit set.
/// </summary>
public enum AlarmSeverity
{
    Information = 0,
    Warning = 1,
    Minor = 2,
    Major = 3,
    Critical = 4
}

/// <summary>
/// <see cref="Active"/> and <see cref="Acknowledged"/> are both "still present" -
/// only <see cref="Cleared"/> means the underlying condition has gone away.
/// </summary>
public enum AlarmState
{
    Active = 0,
    Acknowledged = 1,
    Cleared = 2
}
