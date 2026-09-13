namespace Backend.Domain.Entities.History;

/// <summary>
/// Operator / security activity trail (ERD UserActivityLogs).
/// Timescale hypertable on <c>created_at</c>. Mapped to <c>history.user_activity_logs</c>.
/// Complements <c>scada.system_audit_logs</c> (richer HTTP/security audit from BE 3.1a).
/// </summary>
public class UserActivityLog
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
