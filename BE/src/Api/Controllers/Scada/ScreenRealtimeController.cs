using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Application.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>
/// Snapshot + history by screen mapping (Excel → tag_screen_mapping).
/// Requires authenticated SCADA session (JWT).
/// </summary>
[ApiController]
[Route("api/v1/screens/{screen}")]
[Produces("application/json")]
[Authorize]
public class ScreenRealtimeController(IScreenRealtimeQueryService screens) : ControllerBase
{
    /// <summary>
    /// Initial snapshot for a screen. FE should call this then subscribe SignalR.
    /// Status : 200 OK | 400 | 404 | 500
    /// </summary>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(ApiResponse<RealtimeSnapshotDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<RealtimeSnapshotDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RealtimeSnapshotDto>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> Snapshot(string screen, [FromQuery] ScreenTagQuery query, CancellationToken cancellationToken)
    {
        if (!ScadaScreenMapping.TryParse(screen, out var type))
            return BadRequest(ApiResponse<RealtimeSnapshotDto>.Fail("Unknown screen."));

        return this.ToActionResult(
            await screens.GetSnapshotAsync(type, query, cancellationToken),
            "Snapshot màn hình.");
    }

    /// <summary>
    /// History for tags mapped to Trend / Báo cáo (or any screen). Source = history_* tables.
    /// Status : 200 OK | 400 | 404 | 500
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScreenHistoryPointDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScreenHistoryPointDto>>), ScadaHttpStatuses.BadRequest)]
    public async Task<IActionResult> History(string screen, [FromQuery] ScreenHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!ScadaScreenMapping.TryParse(screen, out var type))
            return BadRequest(ApiResponse<PaginationResult<ScreenHistoryPointDto>>.Fail("Unknown screen."));

        return this.ToActionResult(
            await screens.GetHistoryAsync(type, query, cancellationToken),
            "Lịch sử theo mapping màn hình.");
    }
}
