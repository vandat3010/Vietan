using Backend.Shared.Constants;

namespace Backend.Application.DTOs.Scada;

/// <summary>Catalog thông số vận hành màn Devices — alias khớp Excel TLHN.</summary>
public static class DeviceMonitorParameterCatalog
{
    public sealed record Definition(string Key, string[] Aliases);

    public static IReadOnlyList<Definition> Definitions { get; } =
    [
        new("tempCoilA", ["FB_TEMP_PHASEA", "TEMP_PHASEA", "TEMP_COIL_A", "TEMP_A", "WINDING_A", "COIL_A", "PHASEA"]),
        new("tempCoilB", ["FB_TEMP_PHASEB", "TEMP_PHASEB", "TEMP_COIL_B", "TEMP_B", "WINDING_B", "COIL_B", "PHASEB"]),
        new("tempCoilC", ["FB_TEMP_PHASEC", "TEMP_PHASEC", "TEMP_COIL_C", "TEMP_C", "WINDING_C", "COIL_C", "PHASEC"]),
        new("tempCoilAMax", ["SET_TEMPA", "TEMP_COIL_A_MAX", "TEMP_A_MAX", "WINDING_A_MAX", "COIL_A_MAX"]),
        new("tempCoilBMax", ["SET_TEMPB", "TEMP_COIL_B_MAX", "TEMP_B_MAX", "WINDING_B_MAX", "COIL_B_MAX"]),
        new("tempCoilCMax", ["SET_TEMPC", "TEMP_COIL_C_MAX", "TEMP_C_MAX", "WINDING_C_MAX", "COIL_C_MAX"]),
        new("bearingTop", ["FB_TEMP_NDEBEARING", "TEMP_NDEBEARING", "BEARING_TOP", "BEARING_UPPER", "O_BI_TREN", "NDEBEARING"]),
        new("bearingTopMax", ["SET_TEMPNDE", "BEARING_TOP_MAX", "BEARING_UPPER_MAX", "O_BI_TREN_MAX"]),
        new("bearingBottom", ["FB_TEMP_DEBEARING", "TEMP_DEBEARING", "BEARING_BOTTOM", "BEARING_LOWER", "O_BI_DUOI", "DEBEARING"]),
        new("bearingBottomMax", ["SET_TEMPDE", "BEARING_BOTTOM_MAX", "BEARING_LOWER_MAX", "O_BI_DUOI_MAX"]),
        new("levelRiver", ["RIVER", "LEVEL_RIVER", "WATER_RIVER", "MUC_NUOC_SONG"]),
        new("levelBasin", ["DISCHARGE1", "DISCHARGE", "LEVEL_BASIN", "LEVEL_DISCHARGE", "WATER_BASIN", "MUC_NUOC_BE_XA"]),
        new("runtimeInstant", ["TIME_RUN_M", "RUNTIME_INSTANT", "RUNTIME_NOW", "RUN_INSTANT", "T_GIAN_TUC_THOI"]),
        new("runtimeTotal", ["TOTAL_TIME_RUN_M", "TOTAL_TIME_RUN_H", "RUNTIME", "RUNTIME_H", "RUNTIME_TOTAL", "RUN_HOURS", "HOURS", "T_GIAN"]),
        new("ratedPowerKw", ["RATED_POWER", "RATED_KW", "POWER_RATED", "PN"]),
        new("motorStatus", ["FB_RUN", "MOTOR_STATUS", "STATUS", "PUMP_STATUS", "M_STATUS"]),
        new("faultStatus", ["FB_FAULT", "FB_FAULT_SS", "FB_FAULT_TEMP", "FB_FAULT_V", "FB_FAULT_CURENT", "FB_FAULT_CURRENT"]),
        new("stopStatus", ["FB_STOP"]),
        new("maintenanceStatus", ["FB_MAINTENANCE"]),
    ];

    public static bool Matches(Definition def, string? raw) =>
        ElectricalParameterCatalog.Matches(
            new ElectricalParameterCatalog.Definition(def.Key, def.Key, null, def.Aliases),
            raw);

    public static string StatusLabel(string statusCode) => statusCode switch
    {
        ScadaStatusCodes.Running => "Bơm đang chạy",
        ScadaStatusCodes.Stopped => "Bơm đang dừng",
        ScadaStatusCodes.Error => "Bơm đang lỗi",
        ScadaStatusCodes.Maintenance => "Bơm bảo trì",
        _ => "Không xác định"
    };
}
