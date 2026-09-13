namespace Backend.Shared.Constants;

/// <summary>
/// Well-known role names. Kept as plain constants (not an enum) because roles are
/// data-driven (stored in the Roles table) and new ones can be added without a
/// code change; these constants only cover the roles the system relies on internally.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
}

/// <summary>
/// Fine-grained permission keys used by <c>[Authorize(Policy = Permissions.Users.Create)]</c>.
/// Format: "{Module}.{Action}".
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Delete = "Users.Delete";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Manage = "Roles.Manage";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
    }
}

/// <summary>Custom JWT claim types used in addition to the standard <see cref="System.Security.Claims.ClaimTypes"/>.</summary>
public static class ClaimTypesExtended
{
    public const string Permission = "permission";
    public const string UserId = "uid";
    public const string TokenVersion = "tv";
    public const string SessionId = "sid";
}

public static class PolicyNames
{
    public const string RequireAdmin = "RequireAdmin";
}

/// <summary>Security-related tuning values (token lifetimes, hashing cost, lockout policy).</summary>
public static class SecurityConstants
{
    public const int PasswordMinLength = 8;

    /// <summary>Upper bound on any submitted password (T4.4 §15) — guards against
    /// oversized-payload / expensive-hash (Argon2) denial-of-service.</summary>
    public const int PasswordMaxLength = 128;

    public const int BCryptWorkFactor = 12;
    public const int MaxFailedLoginAttempts = 5;
    public const int LockoutDurationMinutes = 15;
    public const int AccessTokenDefaultExpirationMinutes = 15;
    public const int RefreshTokenDefaultExpirationDays = 7;
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
}
