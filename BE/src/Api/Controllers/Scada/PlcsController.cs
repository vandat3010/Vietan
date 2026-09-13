using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API PLC (scada.plc).</summary>
[ApiController]
[Route("api/v1/plcs")]
[Produces("application/json")]
public class PlcsController(IPlcQueryService plcs) : ControllerBase
{
    /// <summary>
    /// Danh sách PLC.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<PlcDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<PlcDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] PlcQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await plcs.GetPagedAsync(query, cancellationToken), ScadaApiMessages.PlcsListOk);

    /// <summary>
    /// Chi tiết 1 PLC theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PlcDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PlcDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PlcDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await plcs.GetByIdAsync(id, cancellationToken), ScadaApiMessages.PlcDetailOk);
}
