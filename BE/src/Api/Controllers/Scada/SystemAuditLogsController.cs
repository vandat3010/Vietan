using Backend.Application.DTOs.Audit;
using Backend.Application.Interfaces.Services;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>
/// BE 3.1a — Admin-only System Audit Log query (schema <c>scada.system_audit_logs</c>).
/// Read-only: entries are created by the audit subsystem, never by a client.
/// Default sort is newest first; filters + pagination are enforced server-side.
/// </summary>
[ApiController]
[Route("api/v1/system-audit-logs")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
public class SystemAuditLogsController(ISystemAuditLogQueryService auditLogs) : ControllerBase
{
    /// <summary>
    /// Trang danh sách audit, ví dụ
    /// <c>?eventType=Authentication&amp;status=Failed&amp;fromUtc=2026-01-01&amp;pageSize=50</c>.
    /// Mặc định mới nhất trước.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<SystemAuditLogDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<SystemAuditLogDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] SystemAuditLogQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await auditLogs.GetPagedAsync(query, cancellationToken), "Danh sách system audit log.");

    /// <summary>Chi tiết 1 audit log theo Id.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SystemAuditLogDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<SystemAuditLogDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SystemAuditLogDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await auditLogs.GetByIdAsync(id, cancellationToken), "Chi tiết system audit log.");
}
