using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Industrial;

/// <summary>
/// An entry in the industrial event/audit journal (operator actions, mode changes,
/// connection drops). Separate from alarms because events have no lifecycle - they
/// are facts that happened, never acknowledged or cleared.
/// </summary>
public class IndustrialEventDto
{
    public Guid Id { get; set; }

    /// <summary>Originating subsystem, gateway or device, so a flood can be traced to one source.</summary>
    public string Source { get; set; } = default!;

    /// <summary>Machine-readable category used for filtering and retention rules, unlike the free-text <see cref="Message"/>.</summary>
    public string EventType { get; set; } = default!;

    public string Message { get; set; } = default!;
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Null for machine-generated events; set only when the event was caused by an operator.</summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Open-ended payload so new event types can carry context without a schema
    /// change; kept as strings because this journal is for reading and filtering,
    /// not for computation.
    /// </summary>
    public IDictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Inherits <see cref="PaginationRequest"/> because the event journal grows without
/// bound and must never be returned unpaged.
/// </summary>
public class EventLogQuery : PaginationRequest
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    /// <summary>Null means all sources.</summary>
    public string? Source { get; set; }

    /// <summary>Null means all event types.</summary>
    public string? EventType { get; set; }
}
