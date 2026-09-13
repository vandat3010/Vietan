using System.Security.Claims;
using Backend.Application.Common;
using Backend.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace Backend.Infrastructure.Common;

/// <summary>
/// Reads the authenticated user's identity out of the current HTTP context's
/// ClaimsPrincipal (populated by the JWT bearer authentication handler).
/// This is the only place in the whole solution that touches IHttpContextAccessor,
/// which is what lets the Application/Domain layers stay ASP.NET-agnostic.
/// <para>
/// Lives in Infrastructure/Common (not Infrastructure/Identity) because it is a
/// cross-cutting ambient-context service consumed by persistence (audit stamping),
/// services and controllers alike - Identity is reserved for authentication
/// mechanics (JWT issuing, password hashing).
/// </para>
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypesExtended.UserId)?.Value
                ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public long? OperatorUserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Principal?.FindFirst("sub")?.Value;
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public string? SessionId =>
        Principal?.FindFirst(ClaimTypesExtended.SessionId)?.Value
        ?? Principal?.FindFirst("sid")?.Value;

    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Username => Principal?.FindFirst(ClaimTypes.Name)?.Value ?? Email;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll(ClaimTypesExtended.Permission).Select(c => c.Value).ToList() ?? [];

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool HasPermission(string permission) =>
        Principal?.FindAll(ClaimTypesExtended.Permission).Any(c => c.Value == permission) ?? false;
}
