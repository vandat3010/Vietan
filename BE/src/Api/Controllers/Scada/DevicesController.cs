using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API thiết bị (scada.device).</summary>
[ApiController]
[Route("api/v1/devices")]
[Produces("application/json")]
[Authorize]
public class DevicesController(IDeviceQueryService devices) : ControllerBase
{
    /// <summary>
    /// Danh sách thiết bị.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<DeviceDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<DeviceDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] DeviceQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await devices.GetPagedAsync(query, cancellationToken), ScadaApiMessages.DevicesListOk);

    /// <summary>
    /// Chi tiết 1 thiết bị theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<DeviceDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<DeviceDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DeviceDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await devices.GetByIdAsync(id, cancellationToken), ScadaApiMessages.DeviceDetailOk);
}
