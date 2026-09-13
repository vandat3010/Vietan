using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API app setting SCADA (scada.app_setting).</summary>
[ApiController]
[Route("api/v1/app-settings")]
[Produces("application/json")]
public class AppSettingsController(IAppSettingQueryService settings) : ControllerBase
{
    /// <summary>
    /// Danh sách app setting.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppSettingDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppSettingDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        this.ToActionResult(await settings.GetAllAsync(cancellationToken), "Danh sách app-setting.");

    /// <summary>
    /// Chi tiết 1 app-setting theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await settings.GetByIdAsync(id, cancellationToken), "Chi tiết app-setting.");

    /// <summary>
    /// Chi tiết 1 app-setting theo settingKey.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("by-key/{settingKey}")]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetByKey(string settingKey, CancellationToken cancellationToken) =>
        this.ToActionResult(await settings.GetByKeyAsync(settingKey, cancellationToken), "Chi tiết app-setting theo key.");
}
