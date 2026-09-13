using Backend.Application.DTOs.Scada;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services.Scada;

public interface IStationQueryService
{
    Task<Result<PaginationResult<StationDto>>> GetPagedAsync(StationQuery query, CancellationToken cancellationToken = default);
    Task<Result<StationDetailDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Lấy thông số điện các bơm theo stationId.</summary>
    Task<Result<StationElectricalDto>> GetElectricalAsync(long stationId, StationElectricalQuery query, CancellationToken cancellationToken = default);

    /// <summary>Lấy dữ liệu sơ đồ nguyên lý theo stationId.</summary>
    Task<Result<StationSchematicDto>> GetSchematicAsync(long stationId, CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách card thiết bị (plc + device + tag + history) theo stationId.</summary>
    Task<Result<StationDeviceCardsDto>> GetDeviceCardsAsync(long stationId, StationDeviceCardsQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh sách bơm theo stationId: stationId + plc + device + parameters (vận hành) + electrical (object riêng).
    /// Map đủ cột từ scada.station / plc / device / tag + history.history_1s.
    /// </summary>
    Task<Result<StationDeviceMonitorDto>> GetDeviceMonitorAsync(long stationId, StationDeviceMonitorQuery query, CancellationToken cancellationToken = default);

    /// <summary>Báo cáo mức nước (pivot theo thời gian) — phân trang theo dòng thời gian.</summary>
    Task<Result<PaginationResult<WaterLevelReportRowDto>>> GetWaterLevelReportAsync(
        long stationId,
        StationWaterLevelReportQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Dropdown Thiết bị màn báo cáo: Level + Pump1–10 + đồng hồ (id + name).</summary>
    Task<Result<IReadOnlyList<StationReportDeviceOptionDto>>> GetReportDeviceOptionsAsync(
        long stationId,
        CancellationToken cancellationToken = default);

    /// <summary>Bảng báo cáo theo thiết bị — pivot tag từ <c>history_30m</c>.</summary>
    Task<Result<StationReportTableDto>> GetReportTableAsync(
        long stationId,
        StationReportTableQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Dropdown thiết bị màn lịch sử sự kiện (giống báo cáo).</summary>
    Task<Result<IReadOnlyList<StationReportDeviceOptionDto>>> GetEventDeviceOptionsAsync(
        long stationId,
        CancellationToken cancellationToken = default);

    /// <summary>Lịch sử sự kiện theo station — nguồn <c>alarm_history</c>.</summary>
    Task<Result<PaginationResult<StationEventHistoryRowDto>>> GetEventHistoryAsync(
        long stationId,
        StationEventHistoryQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Báo cáo nhiệt độ bơm (history_30s) — phân trang trong ngày.</summary>
    Task<Result<PaginationResult<PumpTemperatureReportRowDto>>> GetPumpTemperatureReportAsync(
        long stationId,
        StationPumpTemperatureReportQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Dropdown thiết bị (Pump1–10) cho màn đồ thị.</summary>
    Task<Result<IReadOnlyList<StationChartDeviceOptionDto>>> GetChartDevicesAsync(
        long stationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lịch sử nhiều series cho chart nhiệt/dòng (1 query history, group theo tag).
    /// Threshold lấy last-value Redis/history (đường ngang ổn định).
    /// </summary>
    Task<Result<StationChartHistoryDto>> GetChartHistoryAsync(
        long stationId,
        long deviceId,
        string chart,
        StationChartHistoryQuery query,
        CancellationToken cancellationToken = default);
}

public interface IPlcQueryService
{
    Task<Result<PaginationResult<PlcDto>>> GetPagedAsync(PlcQuery query, CancellationToken cancellationToken = default);
    Task<Result<PlcDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface IDeviceQueryService
{
    Task<Result<PaginationResult<DeviceDto>>> GetPagedAsync(DeviceQuery query, CancellationToken cancellationToken = default);
    Task<Result<DeviceDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface ITagQueryService
{
    Task<Result<PaginationResult<TagDto>>> GetPagedAsync(TagQuery query, CancellationToken cancellationToken = default);
    Task<Result<TagDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface IHistoryProfileQueryService
{
    Task<Result<IReadOnlyList<HistoryProfileDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<HistoryProfileDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface ITagHistoryConfigQueryService
{
    Task<Result<PaginationResult<TagHistoryConfigDto>>> GetPagedAsync(TagHistoryConfigQuery query, CancellationToken cancellationToken = default);
    Task<Result<TagHistoryConfigDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface ICommunicationConfigQueryService
{
    Task<Result<IReadOnlyList<CommunicationConfigDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CommunicationConfigDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface IMqttConfigQueryService
{
    Task<Result<IReadOnlyList<MqttConfigDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<MqttConfigDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface IScadaUserQueryService
{
    Task<Result<PaginationResult<ScadaUserDto>>> GetPagedAsync(ScadaUserQuery query, CancellationToken cancellationToken = default);
    Task<Result<ScadaUserDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Tạo user SCADA (admin) — không phát JWT.</summary>
    Task<Result<ScadaUserDto>> CreateAsync(CreateScadaUserRequest request, CancellationToken cancellationToken = default);
}

public interface IAppSettingQueryService
{
    Task<Result<IReadOnlyList<AppSettingDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<AppSettingDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<AppSettingDto>> GetByKeyAsync(string settingKey, CancellationToken cancellationToken = default);

    /// <summary>Cấu hình idle timeout phiên (key <c>session.idleTimeoutMinutes</c>).</summary>
    Task<Result<SessionPolicyDto>> GetSessionPolicyAsync(CancellationToken cancellationToken = default);

    /// <summary>Cập nhật idle timeout phiên — upsert app_settings.</summary>
    Task<Result<SessionPolicyDto>> UpdateSessionPolicyAsync(
        UpdateSessionPolicyRequest request,
        CancellationToken cancellationToken = default);
}
