using Backend.Application.DTOs.Audit;
using Backend.Application.Interfaces.Services;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// IAM audit trail (GET) + client-reported events (POST).
/// Security-sensitive actions must be written by backend services — POST rejects them.
/// </summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Produces("application/json")]
public class AuditLogsController(
    IAuditService auditService,
    IClientAuditService clientAudit) : ControllerBase
{
    /// <summary>Paged IAM entity-change trail — Admin only.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin + "," + ScadaRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var result = await auditService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginationResult<AuditLogDto>>.Ok(result.Value!));
    }

    /// <summary>
    /// FE client event. Actor/IP from JWT — spoofed user fields ignored.
    /// Login / password / export / config / alarm commands → 403 (backend-owned).
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ClientAuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ClientAuditLogResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ClientAuditLogResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ClientAuditLogResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateClientEvent(
        [FromBody] CreateClientAuditLogRequest request,
        CancellationToken cancellationToken)
    {
        var result = await clientAudit.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
            return Ok(ApiResponse<ClientAuditLogResponse>.Ok(result.Value!, "Client audit recorded."));

        var body = ApiResponse<ClientAuditLogResponse>.Fail(
            result.Errors.FirstOrDefault() ?? result.ErrorCode,
            result.Errors);

        if (result.ErrorCode.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, body);
        if (result.ErrorCode.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(body);
        return BadRequest(body);
    }
}
