using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API cấu hình giao tiếp (scada.communication_config) — Admin only.</summary>
[ApiController]
[Route("api/v1/communication-configs")]
[Produces("application/json")]
[Authorize(Roles = ScadaRoles.AdminOnly)]
public class CommunicationConfigsController(ICommunicationConfigQueryService configs) : ControllerBase
{
    /// <summary>
    /// Danh sách cấu hình giao tiếp.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommunicationConfigDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommunicationConfigDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        this.ToActionResult(await configs.GetAllAsync(cancellationToken), "Danh sách communication-config.");

    /// <summary>
    /// Chi tiết 1 communication-config theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CommunicationConfigDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<CommunicationConfigDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CommunicationConfigDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await configs.GetByIdAsync(id, cancellationToken), "Chi tiết communication-config.");
}
