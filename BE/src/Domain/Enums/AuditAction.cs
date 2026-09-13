namespace Backend.Domain.Enums;

/// <summary>
/// What an <see cref="Entities.AuditLog"/> entry describes. Covers both entity
/// mutations and the security events auditors always ask for (logins, exports).
/// </summary>
public enum AuditAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Restored = 3,
    Login = 4,
    Logout = 5,
    PasswordChanged = 6,
    PermissionChanged = 7,
    Exported = 8,
    Imported = 9
}
