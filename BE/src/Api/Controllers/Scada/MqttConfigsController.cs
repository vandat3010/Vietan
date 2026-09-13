using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API cấu hình MQTT (scada.mqtt_config).</summary>
[ApiController]
[Route("api/v1/mqtt-configs")]
[Produces("application/json")]
public class MqttConfigsController(IMqttConfigQueryService configs) : ControllerBase
{
    /// <summary>
    /// Danh sách cấu hình MQTT.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MqttConfigDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MqttConfigDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        this.ToActionResult(await configs.GetAllAsync(cancellationToken), "Danh sách mqtt-config.");

    /// <summary>
    /// Chi tiết 1 mqtt-config theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<MqttConfigDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<MqttConfigDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MqttConfigDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await configs.GetByIdAsync(id, cancellationToken), "Chi tiết mqtt-config.");
}
