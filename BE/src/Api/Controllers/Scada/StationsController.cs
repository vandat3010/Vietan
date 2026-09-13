using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API trạm bơm (scada.station) + thông tin điện / sơ đồ nguyên lý / thẻ thiết bị theo stationId.</summary>
[ApiController]
[Route("api/v1/stations")]
[Produces("application/json")]
public class StationsController(IStationQueryService stations) : ControllerBase
{
    /// <summary>
    /// Danh sách trạm bơm.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<StationDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<StationDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] StationQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetPagedAsync(query, cancellationToken), ScadaApiMessages.StationsListOk);

    /// <summary>
    /// Chi tiết 1 trạm theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<StationDetailDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<StationDetailDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StationDetailDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetByIdAsync(id, cancellationToken), ScadaApiMessages.StationDetailOk);

    /// <summary>
    /// Thông số điện của thiết bị theo trạm.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{stationId:long}/electrical")]
    [ProducesResponseType(typeof(ApiResponse<StationElectricalDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<StationElectricalDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StationElectricalDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetElectrical(long stationId, [FromQuery] StationElectricalQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetElectricalAsync(stationId, query, cancellationToken), ScadaApiMessages.StationElectricalOk);

    /// <summary>
    /// Sơ đồ nguyên lý theo trạm.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{stationId:long}/schematic")]
    [ProducesResponseType(typeof(ApiResponse<StationSchematicDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<StationSchematicDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StationSchematicDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetSchematic(long stationId, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetSchematicAsync(stationId, cancellationToken), ScadaApiMessages.StationSchematicOk);

    /// <summary>
    /// Thiết bị theo trạm (PLC + thiết bị + tag + lịch sử).
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{stationId:long}/device-cards")]
    [ProducesResponseType(typeof(ApiResponse<StationDeviceCardsDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<StationDeviceCardsDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StationDeviceCardsDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetDeviceCards(long stationId, [FromQuery] StationDeviceCardsQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetDeviceCardsAsync(stationId, query, cancellationToken), ScadaApiMessages.StationDeviceCardsOk);

    /// <summary>
    /// Danh sách bơm màn Devices (đủ thông số UI).
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{stationId:long}/device-monitor")]
    [ProducesResponseType(typeof(ApiResponse<StationDeviceMonitorDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<StationDeviceMonitorDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StationDeviceMonitorDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetDeviceMonitor(long stationId, [FromQuery] StationDeviceMonitorQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetDeviceMonitorAsync(stationId, query, cancellationToken), ScadaApiMessages.StationDeviceMonitorOk);

    /// <summary>
    /// Dropdown Thiết bị màn báo cáo: Level + Pump1–10 + đồng hồ (id + name).
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{stationId:long}/reports/devices")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationReportDeviceOptionDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationReportDeviceOptionDto>>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationReportDeviceOptionDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetReportDevices(long stationId, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetReportDeviceOptionsAsync(stationId, cancellationToken), ScadaApiMessages.StationReportDevicesOk);

    /// <summary>
    /// Bảng báo cáo theo thiết bị (history_30m): cột theo tag của device, lọc ngày + giờ.
    /// Query: deviceId, reportDate, startTime, endTime, pageNumber, pageSize.
    /// </summary>
    [HttpGet("{stationId:long}/reports/table")]
    [ProducesResponseType(typeof(ApiResponse<StationReportTableDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<StationReportTableDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StationReportTableDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StationReportTableDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetReportTable(
        long stationId,
        [FromQuery] StationReportTableQuery query,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await stations.GetReportTableAsync(stationId, query, cancellationToken),
            ScadaApiMessages.StationReportTableOk);

    /// <summary>
    /// Dropdown thiết bị màn lịch sử sự kiện (Level + Pump + đồng hồ).
    /// </summary>
    [HttpGet("{stationId:long}/events/devices")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationReportDeviceOptionDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationReportDeviceOptionDto>>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> GetEventDevices(long stationId, CancellationToken cancellationToken) =>
        this.ToActionResult(
            await stations.GetEventDeviceOptionsAsync(stationId, cancellationToken),
            ScadaApiMessages.StationEventDevicesOk);

    /// <summary>
    /// Lịch sử sự kiện theo trạm — <c>alarm_history</c> (station → device → tag).
    /// Query: category=status|value-change|system, deviceId, fromDate, toDate, keyword, pageNumber, pageSize.
    /// </summary>
    [HttpGet("{stationId:long}/events/history")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<StationEventHistoryRowDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<StationEventHistoryRowDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<StationEventHistoryRowDto>>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> GetEventHistory(
        long stationId,
        [FromQuery] StationEventHistoryQuery query,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await stations.GetEventHistoryAsync(stationId, query, cancellationToken),
            ScadaApiMessages.StationEventHistoryOk);

    /// <summary>
    /// Báo cáo mức nước (history_30s): thời gian + sông + mức xả 1..10, phân trang trong ngày.
    /// Status : 200 OK | 400 Yêu cầu không hợp lệ | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{stationId:long}/reports/water-levels")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<WaterLevelReportRowDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<WaterLevelReportRowDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<WaterLevelReportRowDto>>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<WaterLevelReportRowDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetWaterLevelReport(long stationId, [FromQuery] StationWaterLevelReportQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetWaterLevelReportAsync(stationId, query, cancellationToken), ScadaApiMessages.StationWaterLevelReportOk);

    /// <summary>
    /// Báo cáo nhiệt độ bơm (history_30s): thời gian, bơm, nhiệt A/B/C, ổ bi trên/dưới.
    /// Status : 200 OK | 400 Yêu cầu không hợp lệ | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{stationId:long}/reports/pump-temperatures")]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<PumpTemperatureReportRowDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<PumpTemperatureReportRowDto>>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<PumpTemperatureReportRowDto>>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<PumpTemperatureReportRowDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetPumpTemperatureReport(long stationId, [FromQuery] StationPumpTemperatureReportQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetPumpTemperatureReportAsync(stationId, query, cancellationToken), ScadaApiMessages.StationPumpTemperatureReportOk);

    /// <summary>
    /// Dropdown thiết bị (Pump1–10) cho màn đồ thị nhiệt / dòng.
    /// </summary>
    [HttpGet("{stationId:long}/charts/devices")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationChartDeviceOptionDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationChartDeviceOptionDto>>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> GetChartDevices(long stationId, CancellationToken cancellationToken) =>
        this.ToActionResult(await stations.GetChartDevicesAsync(stationId, cancellationToken), ScadaApiMessages.StationChartDevicesOk);

    /// <summary>
    /// Lịch sử chart (nhiều series / 1 request). Threshold = last-value ổn định; measured = history.
    /// Query: from, to (DateTimeOffset bắt buộc), interval=1s|30s|1m.
    /// </summary>
    [HttpGet("{stationId:long}/devices/{deviceId:long}/charts/{chart}/history")]
    [ProducesResponseType(typeof(ApiResponse<StationChartHistoryDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<StationChartHistoryDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StationChartHistoryDto>), ScadaHttpStatuses.NotFound)]
    public async Task<IActionResult> GetChartHistory(
        long stationId,
        long deviceId,
        string chart,
        [FromQuery] StationChartHistoryQuery query,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await stations.GetChartHistoryAsync(stationId, deviceId, chart, query, cancellationToken),
            ScadaApiMessages.StationChartHistoryOk);
}
