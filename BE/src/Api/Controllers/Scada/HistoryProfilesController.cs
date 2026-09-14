using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API cấu hình profile ghi lịch sử (scada.history_profile) — Admin only.</summary>
[ApiController]
[Route("api/v1/history-profiles")]
[Produces("application/json")]
[Authorize(Roles = ScadaRoles.AdminOnly)]
public class HistoryProfilesController(IHistoryProfileQueryService profiles) : ControllerBase
{
    /// <summary>
    /// Danh sách history profile.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HistoryProfileDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HistoryProfileDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        this.ToActionResult(await profiles.GetAllAsync(cancellationToken), "Danh sách history profile.");

    /// <summary>
    /// Chi tiết 1 history profile theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<HistoryProfileDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<HistoryProfileDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<HistoryProfileDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await profiles.GetByIdAsync(id, cancellationToken), "Chi tiết history profile.");
}
