namespace Backend.Application.DTOs.Scada;

/// <summary>
/// Cột báo cáo theo loại thiết bị (Excel Trend/Báo cáo) — map Code → key/header.
/// </summary>
public static class StationReportTagCatalog
{
    public sealed record ColumnDef(string Key, string Header, string[] Aliases);

    /// <summary>Device Level / Sensor — khớp UI Mức nước sông + Mức bể xả.</summary>
    public static IReadOnlyList<ColumnDef> WaterLevelColumns { get; } =
    [
        new("riverLevel", "Mức nước sông",
            ["RIVER", "LEVEL_RIVER", "WATER_RIVER", "MUC_NUOC_SONG"]),
        new("dischargeTankLevel", "Mức bể xả",
            ["DISCHARGE1", "DISCHARGE_1", "LEVEL_DISCHARGE_1", "LEVEL_XA_1", "MUC_XA_1", "LEVEL_BASIN"]),
    ];

    public static IReadOnlyList<ColumnDef> PumpTemperatureColumns { get; } =
    [
        new("tempA", "Nhiệt độ A",
            ["FB_TEMP_PHASEA", "TEMP_PHASEA", "TEMP_COIL_A", "TEMP_A", "WINDING_A", "COIL_A"]),
        new("tempB", "Nhiệt độ B",
            ["FB_TEMP_PHASEB", "TEMP_PHASEB", "TEMP_COIL_B", "TEMP_B", "WINDING_B", "COIL_B"]),
        new("tempC", "Nhiệt độ C",
            ["FB_TEMP_PHASEC", "TEMP_PHASEC", "TEMP_COIL_C", "TEMP_C", "WINDING_C", "COIL_C"]),
        new("bearingBottom", "Nhiệt độ bi dưới",
            ["FB_TEMP_DEBEARING", "TEMP_DEBEARING", "BEARING_BOTTOM", "O_BI_DUOI"]),
        new("bearingTop", "Nhiệt độ bi trên",
            ["FB_TEMP_NDEBEARING", "TEMP_NDEBEARING", "BEARING_TOP", "O_BI_TREN"]),
    ];

    public static IReadOnlyList<ColumnDef> InputMeterColumns { get; } =
    [
        new("voltageRS", "Điện áp RS", ["U12", "VOLTAGE_RS", "U_RS", "URS"]),
        new("voltageST", "Điện áp ST", ["U23", "VOLTAGE_ST", "U_ST", "UST"]),
        new("voltageRT", "Điện áp RT", ["U31", "VOLTAGE_TR", "VOLTAGE_RT", "U_TR", "UTR"]),
        new("currentR", "Dòng R", ["I1", "CURRENT_R", "IA"]),
        new("currentS", "Dòng S", ["I2", "CURRENT_S", "IB"]),
        new("currentT", "Dòng T", ["I3", "CURRENT_T", "IC"]),
        new("currentAvg", "Dòng trung bình", ["I_PH", "CURRENT", "CURRENT_A"]),
        new("power", "Công suất", ["TOTAL_KW", "POWER_KW", "CONG_SUAT"]),
        new("pf", "PF", ["POWER_FACTOR", "PF", "COSPHI", "COS_PHI"]),
    ];

    public static IReadOnlyList<ColumnDef> ResolveColumns(string? deviceType, string? deviceCode)
    {
        var type = (deviceType ?? string.Empty).Trim();
        var code = (deviceCode ?? string.Empty).Trim();

        if (string.Equals(type, "Sensor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "Level", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Level", StringComparison.OrdinalIgnoreCase))
            return WaterLevelColumns;

        if (string.Equals(type, "Pump", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("Pump", StringComparison.OrdinalIgnoreCase))
            return PumpTemperatureColumns;

        if (string.Equals(type, "PowerMeter", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Meter", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Metter", StringComparison.OrdinalIgnoreCase))
            return InputMeterColumns;

        return WaterLevelColumns;
    }

    public static bool Matches(ColumnDef def, string? raw) =>
        ElectricalParameterCatalog.Matches(
            new ElectricalParameterCatalog.Definition(def.Key, def.Header, null, def.Aliases),
            raw);
}
