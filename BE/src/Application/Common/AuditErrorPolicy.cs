using Backend.Domain.Enums;

namespace Backend.Application.Common;

/// <summary>
/// BE 3.1a §4 — decides which HTTP failures are worth a System Audit row, so the
/// global exception handler does not flood the audit table with routine 4xx noise.
/// Pure and unit-testable; the exception middleware owns the single call site.
/// </summary>
public static class AuditErrorPolicy
{
    /// <summary>
    /// Audit only security-relevant and system failures:
    /// <list type="bullet">
    /// <item>401 / 403 — authorization/security events.</item>
    /// <item>5xx — server/system errors.</item>
    /// </list>
    /// 400/404/409 validation/not-found/conflict are intentionally NOT audited
    /// (they are handled business responses and would be pure noise).
    /// </summary>
    public static bool ShouldAudit(int statusCode) =>
        statusCode is 401 or 403 || statusCode >= 500;

    /// <summary>5xx → SystemError; 401/403 → ApiError.</summary>
    public static AuditEventType EventTypeFor(int statusCode) =>
        statusCode >= 500 ? AuditEventType.SystemError : AuditEventType.ApiError;

    /// <summary>5xx → SystemError action; otherwise ApiError action.</summary>
    public static string ActionFor(int statusCode) =>
        statusCode >= 500 ? Shared.Constants.AuditActionNames.SystemError : Shared.Constants.AuditActionNames.ApiError;
}
