namespace Backend.Shared.Constants;

/// <summary>
/// Cache key prefixes/templates and default expirations. Centralized so a cache
/// provider swap (in-memory -> Redis, see README "Extensibility") never
/// requires hunting down string literals scattered across services.
/// </summary>
public static class CacheConstants
{
    public const string UserByIdKeyPrefix = "user:id:";
    public const string UserPermissionsKeyPrefix = "user:permissions:";
    public const string RoleByIdKeyPrefix = "role:id:";
    public const string RolesAllKey = "roles:all";

    public const int DefaultAbsoluteExpirationMinutes = 30;
    public const int DefaultSlidingExpirationMinutes = 10;
}
