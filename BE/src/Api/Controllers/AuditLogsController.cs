using Backend.Application.DTOs.Audit;
using Backend.Application.Interfaces.Services;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Read-only view over the audit trail. Write access is deliberately absent:
/// entries are created by services through <see cref="IAuditService"/> as part of
/// the business transaction, never by a client calling an endpoint.
/// </summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
public class AuditLogsController(IAuditService auditService) : ControllerBase
{
    /// <summary>
    /// Paged audit trail, e.g.
    /// <c>?entityName=User&amp;action=Updated&amp;fromUtc=2026-01-01&amp;pageSize=50</c>.
    /// Defaults to newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var result = await auditService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginationResult<AuditLogDto>>.Ok(result.Value!));
    }
}
