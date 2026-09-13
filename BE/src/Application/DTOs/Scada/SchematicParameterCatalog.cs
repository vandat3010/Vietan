using Backend.Shared.Constants;

namespace Backend.Application.DTOs.Scada;

/// <summary>Catalog map tag sơ đồ nguyên lý.</summary>
public static class SchematicParameterCatalog
{
    public sealed record Definition(string Key, string[] Aliases);

    public static IReadOnlyList<Definition> MeasureDefinitions { get; } =
    [
        new("i1", ["I1", "CURRENT_L1", "PHASE_I1", "IA", "METER_I1"]),
        new("i2", ["I2", "CURRENT_L2", "PHASE_I2", "IB", "METER_I2"]),
        new("i3", ["I3", "CURRENT_L3", "PHASE_I3", "IC", "METER_I3"]),
        new("v1", ["U1", "V1", "VOLTAGE_L1", "PHASE_V1", "UA", "VAN_A"]),
        new("v2", ["U2", "V2", "VOLTAGE_L2", "PHASE_V2", "UB", "VAN_B"]),
        new("v3", ["U3", "V3", "VOLTAGE_L3", "PHASE_V3", "UC", "VAN_C"]),
        new("currentA", ["I_PH", "CURRENT", "I_LOAD", "DONG_DIEN", "I_A"]),
        new("runtimeH", ["TOTAL_TIME_RUN_H", "TOTAL_TIME_RUN_M", "TIME_RUN_M", "RUNTIME", "RUNTIME_H", "RUN_HOURS", "T_GIAN", "HOURS"]),
        new("ratedPowerKw", ["RATED_POWER", "RATED_KW", "POWER_RATED", "PN"]),
    ];

    public static IReadOnlyList<Definition> StatusDefinitions { get; } =
    [
        new("motorStatus", ["FB_RUN", "MOTOR_STATUS", "PUMP_STATUS", "M_STATUS"]),
        new("kdmStatus", ["FB_RUN", "KDM_STATUS", "KDM", "STARTER_STATUS"]),
        new("lockStatus", ["LOCK_STATUS", "LOCK", "BREAKER", "MCCB_STATUS"]),
        new("faultStatus", ["FB_FAULT", "FB_FAULT_SS", "FB_FAULT_TEMP", "FB_FAULT_V", "FB_FAULT_CURENT"]),
        new("stopStatus", ["FB_STOP"]),
        new("maintenanceStatus", ["FB_MAINTENANCE"]),
    ];

    public static bool Matches(Definition def, string? raw) =>
        ElectricalParameterCatalog.Matches(
            new ElectricalParameterCatalog.Definition(def.Key, def.Key, null, def.Aliases),
            raw);

    public static string MapMotorStatus(double? value, string? rawCode = null) =>
        ScadaStatusMapper.MapMotorStatus(value, rawCode);

    public static string MapKdmStatus(double? value, string? rawCode = null) =>
        ScadaStatusMapper.MapKdmStatus(value, rawCode);

    public static string MapLockStatus(double? value, string? rawCode = null) =>
        ScadaStatusMapper.MapLockStatus(value, rawCode);

    public static string ResolveKdmStatus(string motorStatus, string kdmStatus) =>
        ScadaStatusMapper.ResolveKdmStatus(motorStatus, kdmStatus);
}
