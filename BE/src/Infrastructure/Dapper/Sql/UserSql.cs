namespace Backend.Infrastructure.Dapper.Sql;

/// <summary>
/// Hand-tuned, read-only SQL used by the Dapper reporting layer. Kept as named
/// constants (instead of inline strings scattered across repositories) so DBAs
/// can review/optimize the exact statements that will run in production.
/// </summary>
public static class UserSql
{
    public const string GetUserSummary = """
        SELECT
            COUNT(*)                                                      AS "TotalUsers",
            COUNT(*) FILTER (WHERE "Status" = 'Active')                   AS "ActiveUsers",
            COUNT(*) FILTER (WHERE "Status" = 'Inactive')                 AS "InactiveUsers",
            COUNT(*) FILTER (WHERE "Status" = 'Locked')                   AS "LockedUsers",
            COUNT(*) FILTER (WHERE "CreatedDate" >= NOW() - INTERVAL '30 days') AS "NewUsersLast30Days"
        FROM app."Users"
        WHERE "IsDeleted" = FALSE;
        """;

    public const string GetTopActiveUsers = """
        SELECT
            u."Id"              AS "UserId",
            u."Email"           AS "Email",
            u."FirstName" || ' ' || u."LastName" AS "FullName",
            u."LastLoginAtUtc"  AS "LastLoginAtUtc",
            0                   AS "LoginCountLast30Days"
        FROM app."Users" u
        WHERE u."IsDeleted" = FALSE
        ORDER BY u."LastLoginAtUtc" DESC NULLS LAST
        LIMIT @Top;
        """;
}
