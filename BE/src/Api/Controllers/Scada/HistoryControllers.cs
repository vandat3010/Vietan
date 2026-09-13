using Backend.Application.DTOs.History;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API mẫu lịch sử tag (history.history_1s / 1m / 30m).</summary>
[ApiController]
[Route("api/v1/history")]
[Produces("application/json")]
public class HistorySamplesController(IHistorySampleQueryService history) : ControllerBase
{
    /// <summary>
    /// Lịch sử tag theo chu kỳ 1 giây.
    /// Status : 200 OK | 400 Yêu cầu không hợp lệ | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("1s")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> Get1s([FromQuery] HistorySampleQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await history.Get1sAsync(query, cancellationToken), "Lịch sử tag 1s.");

    /// <summary>
    /// Lịch sử tag theo chu kỳ 1 phút.
    /// Status : 200 OK | 400 Yêu cầu không hợp lệ | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("1m")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<HistorySampleDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> Get1m([FromQuery] HistorySampleQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await history.Get1mAsync(query, cancellationToken), "Lịch sử tag 1m.");

    /// <summary>
    /// Lịch sử tag theo chu kỳ 30 phút.
    /// Status : 200 OK | 400 Yêu cầu không hợp lệ | 500 Lỗi không mong đợi
    /// </summary>
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
public class AlarmHistoriesController(IAlarmHistoryQueryService alarms) : ControllerBase
{
    /// <summary>
    /// Danh sách alarm history.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AlarmHistoryDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AlarmHistoryDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] AlarmHistoryQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await alarms.GetPagedAsync(query, cancellationToken), "Danh sách alarm history.");

    /// <summary>
    /// Chi tiết 1 alarm history theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AlarmHistoryDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await alarms.GetByIdAsync(id, cancellationToken), "Chi tiết alarm history.");
}

/// <summary>API nhật ký sự kiện SCADA.</summary>
[ApiController]
[Route("api/v1/event-logs")]
[Produces("application/json")]
public class EventLogsController(IScadaEventLogQueryService events) : ControllerBase
{
    /// <summary>
    /// Danh sách event log.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<EventLogDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<EventLogDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] EventLogQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await events.GetPagedAsync(query, cancellationToken), "Danh sách event log.");

    /// <summary>
    /// Chi tiết 1 event log theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
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
public class UserActivityLogsController(IUserActivityLogQueryService logs) : ControllerBase
{
    /// <summary>
    /// Danh sách user activity log.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UserActivityLogDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UserActivityLogDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] UserActivityLogQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await logs.GetPagedAsync(query, cancellationToken), "Danh sách user activity log.");

    /// <summary>
    /// Chi tiết 1 user activity log theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserActivityLogDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<UserActivityLogDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserActivityLogDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await logs.GetByIdAsync(id, cancellationToken), "Chi tiết user activity log.");
}
