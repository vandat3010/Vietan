using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API app setting SCADA — Admin only. Mutation chỉ allowlisted keys.</summary>
[ApiController]
[Route("api/v1/app-settings")]
[Produces("application/json")]
[Authorize(Roles = ScadaRoles.AdminOnly)]
public class AppSettingsController(IAppSettingQueryService settings) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppSettingDto>>), ScadaHttpStatuses.Ok)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        this.ToActionResult(await settings.GetAllAsync(cancellationToken), "Danh sách app-setting.");

    [HttpGet("catalog")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppSettingCatalogItemDto>>), ScadaHttpStatuses.Ok)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken) =>
        this.ToActionResult(await settings.GetCatalogAsync(cancellationToken), "Catalog cấu hình.");

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await settings.GetByIdAsync(id, cancellationToken), "Chi tiết app-setting.");

    [HttpGet("by-key/{settingKey}")]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> GetByKey(string settingKey, CancellationToken cancellationToken) =>
        this.ToActionResult(await settings.GetByKeyAsync(settingKey, cancellationToken), "Chi tiết app-setting theo key.");

    /// <summary>Cập nhật giá trị theo key — chỉ key trong catalog EditableViaApi.</summary>
    [HttpPut("by-key/{settingKey}")]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AppSettingDto>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> UpdateByKey(
        string settingKey,
        [FromBody] UpdateAppSettingRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await settings.UpdateByKeyAsync(settingKey, request, cancellationToken),
            "Đã cập nhật app-setting.");
}
