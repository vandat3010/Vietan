namespace Backend.Application.Common;

/// <summary>
/// Abstraction over "who is making this request". Implemented in Infrastructure
/// using IHttpContextAccessor so the Application layer never takes a direct
/// dependency on ASP.NET Core.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>IAM user id (<c>app.Users</c>, Guid) when present on the token.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// SCADA operator id (<c>scada.users</c>, long) from JWT <c>sub</c> /
    /// <c>NameIdentifier</c>. Used by SCADA authentication flows.
    /// </summary>
    long? OperatorUserId { get; }

    /// <summary>
    /// SCADA concurrent session id from JWT claim <c>sid</c> (issued by BE at login).
    /// </summary>
    string? SessionId { get; }

    /// <summary>Display name claim, falling back to <see cref="Email"/> when absent.</summary>
    string? Username { get; }

    string? Email { get; }

    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Permission claims carried by the token. Exposed as data (not just the
    /// <see cref="HasPermission"/> check) so services can make bulk decisions -
    /// e.g. filtering a menu or a list of allowed actions - without one call per permission.
    /// </summary>
    IReadOnlyList<string> Permissions { get; }

    bool IsAuthenticated { get; }

    string? IpAddress { get; }

    bool HasPermission(string permission);
}
