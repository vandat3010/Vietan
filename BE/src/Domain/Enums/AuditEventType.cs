namespace Backend.Domain.Enums;

/// <summary>
/// BE 3.1a — high-level category of a <see cref="Entities.Scada.SystemAuditLog"/>
/// entry. Kept coarse so the audit trail stays queryable; extend as new event
/// families appear (e.g. DeviceControl, AlarmAck) without changing existing rows.
/// </summary>
public enum AuditEventType
{
    /// <summary>Login / logout / authentication outcomes.</summary>
    Authentication = 0,

    /// <summary>User-initiated data export (Excel/CSV/PDF).</summary>
    DataExport = 1,

    /// <summary>Security/business-relevant API error (e.g. 401/403, important business exception).</summary>
    ApiError = 2,

    /// <summary>Unhandled/system-level failure (5xx, infrastructure error).</summary>
    SystemError = 3
}
