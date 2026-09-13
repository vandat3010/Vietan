namespace Backend.Shared.Constants;

/// <summary>
/// BE 3.1a — canonical <c>Action</c> strings for the System Audit Log. Centralized
/// so callers never hard-code magic strings. Add new actions here (e.g. CreateUser,
/// ChangePassword, ControlDevice) without touching the audit subsystem.
/// </summary>
public static class AuditActionNames
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string Export = "Export";
    public const string ApiError = "ApiError";
    public const string SystemError = "SystemError";

    // T4.1 security events. Stored as strings in scada.system_audit_logs, so adding a
    // new action here can never reorder/break already-persisted rows (unlike an int enum).
    public const string LoginFailed = "LoginFailed";
    public const string AccountLocked = "AccountLocked";
    public const string PasswordChanged = "PasswordChanged";
    public const string ConfigurationChanged = "ConfigurationChanged";
    public const string RefreshTokenReuse = "RefreshTokenReuse";
}
