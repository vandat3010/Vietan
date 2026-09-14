using Backend.Application.DTOs.History;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API mẫu lịch sử tag (history.history_1s / 1m / 30m).</summary>
[ApiController]
[Route("api/v1/history")]
[Produces("application/json")]
[Authorize]
public class HistorySamplesController(IHistorySampleQueryService history) : ControllerBase
{
    [HttpGet("1s")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> Get1s([FromQuery] HistorySampleQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await history.Get1sAsync(query, cancellationToken), "Lịch sử tag 1s.");

    [HttpGet("1m")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> Get1m([FromQuery] HistorySampleQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await history.Get1mAsync(query, cancellationToken), "Lịch sử tag 1m.");

    [HttpGet("30m")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> Get30m([FromQuery] HistorySampleQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await history.Get30mAsync(query, cancellationToken), "Lịch sử tag 30m.");
}

/// <summary>API lịch sử alarm.</summary>
[ApiController]
[Route("api/v1/alarm-histories")]
[Produces("application/json")]
[Authorize]
public class AlarmHistoriesController(IAlarmHistoryQueryService alarms) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AlarmHistoryDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AlarmHistoryDto>>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AlarmHistoryDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] AlarmHistoryQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await alarms.GetPagedAsync(query, cancellationToken), "Danh sách alarm history.");

    /// <summary>
    /// Alarm đang mở toàn hệ thống (<c>EndTime IS NULL</c>).
    /// Lọc thêm stationId / isAcknowledged / deviceId qua query.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AlarmHistoryDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AlarmHistoryDto>>), ScadaHttpStatuses.Unauthorized)]
    public async Task<IActionResult> GetActive([FromQuery] AlarmHistoryQuery query, CancellationToken cancellationToken)
    {
        query.ActiveOnly = true;
        return this.ToActionResult(await alarms.GetPagedAsync(query, cancellationToken), "Danh sách alarm đang mở.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await alarms.GetByIdAsync(id, cancellationToken), "Chi tiết alarm history.");

    /// <summary>Acknowledge alarm — Operator/Admin. Idempotent.</summary>
    [HttpPost("{id:long}/acknowledge")]
    [Authorize(Roles = ScadaRoles.Operator + "," + ScadaRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.Forbidden)]
    public async Task<IActionResult> Acknowledge(
        long id,
        [FromBody] AlarmCommandRequest? request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await alarms.AcknowledgeAsync(id, request?.Note, cancellationToken),
            "Đã xác nhận alarm.");

    /// <summary>Clear open alarm (EndTime = UtcNow) — Operator/Admin. Keeps history row.</summary>
    [HttpPost("{id:long}/clear")]
    [Authorize(Roles = ScadaRoles.Operator + "," + ScadaRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> Clear(
        long id,
        [FromBody] AlarmCommandRequest? request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await alarms.ClearAsync(id, request?.Note, cancellationToken),
            "Đã clear alarm.");
}

/// <summary>API nhật ký sự kiện SCADA.</summary>
[ApiController]
[Route("api/v1/event-logs")]
[Produces("application/json")]
[Authorize]
public class EventLogsController(IScadaEventLogQueryService events) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<EventLogDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<EventLogDto>>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<EventLogDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] EventLogQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await events.GetPagedAsync(query, cancellationToken), "Danh sách event log.");

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<EventLogDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<EventLogDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EventLogDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await events.GetByIdAsync(id, cancellationToken), "Chi tiết event log.");
}

/// <summary>API nhật ký hoạt động người dùng SCADA.</summary>
[ApiController]
[Route("api/v1/user-activity-logs")]
[Produces("application/json")]
[Authorize]
public class UserActivityLogsController(IUserActivityLogQueryService logs) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UserActivityLogDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UserActivityLogDto>>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UserActivityLogDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] UserActivityLogQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await logs.GetPagedAsync(query, cancellationToken), "Danh sách user activity log.");

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserActivityLogDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<UserActivityLogDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserActivityLogDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await logs.GetByIdAsync(id, cancellationToken), "Chi tiết user activity log.");
}
