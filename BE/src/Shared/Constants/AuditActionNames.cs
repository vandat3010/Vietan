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

    public const string CreateUser = "CreateUser";
    public const string UpdateUser = "UpdateUser";
    public const string DeactivateUser = "DeactivateUser";
    public const string UpdateStation = "UpdateStation";

    public const string AcknowledgeAlarm = "AcknowledgeAlarm";
    public const string ClearAlarm = "ClearAlarm";
    public const string CreateLicense = "CreateLicense";
    public const string UpdateLicense = "UpdateLicense";
    public const string DeactivateLicense = "DeactivateLicense";
    public const string CreateMapLayer = "CreateMapLayer";
    public const string UpdateMapLayer = "UpdateMapLayer";
    public const string DeleteMapLayer = "DeleteMapLayer";
    public const string ClientEvent = "ClientEvent";
}
