namespace Backend.Application.DTOs.Scada;

/// <summary>
/// Catalog series cho màn đồ thị nhiệt / dòng.
/// <c>key</c> khớp FE chart (windingA, phaseR, …).
/// </summary>
public static class StationChartCatalog
{
    public const string Temperature = "temperature";
    public const string Current = "current";

    public sealed record SeriesDefinition(
        string Key,
        string Label,
        string Role,
        string[] Aliases);

    /// <summary>role = measured | threshold</summary>
    public static IReadOnlyList<SeriesDefinition> TemperatureSeries { get; } =
    [
        new("windingA", "Nhiệt độ cuộn A", "measured",
            ["FB_TEMP_PHASEA", "TEMP_PHASEA", "TEMP_COIL_A", "TEMP_A", "WINDING_A", "COIL_A", "PHASEA"]),
        new("windingB", "Nhiệt độ cuộn B", "measured",
            ["FB_TEMP_PHASEB", "TEMP_PHASEB", "TEMP_COIL_B", "TEMP_B", "WINDING_B", "COIL_B", "PHASEB"]),
        new("windingC", "Nhiệt độ cuộn C", "measured",
            ["FB_TEMP_PHASEC", "TEMP_PHASEC", "TEMP_COIL_C", "TEMP_C", "WINDING_C", "COIL_C", "PHASEC"]),
        new("bearingTop", "Nhiệt độ bi trên", "measured",
            ["FB_TEMP_NDEBEARING", "TEMP_NDEBEARING", "BEARING_TOP", "BEARING_UPPER", "O_BI_TREN", "NDEBEARING"]),
        new("bearingBottom", "Nhiệt độ bi dưới", "measured",
            ["FB_TEMP_DEBEARING", "TEMP_DEBEARING", "BEARING_BOTTOM", "BEARING_LOWER", "O_BI_DUOI", "DEBEARING"]),
        new("windingAAllowed", "Nhiệt độ cho phép cuộn A", "threshold",
            ["SET_TEMPA", "TEMP_COIL_A_MAX", "TEMP_A_MAX", "WINDING_A_MAX", "COIL_A_MAX"]),
        new("windingBAllowed", "Nhiệt độ cho phép cuộn B", "threshold",
            ["SET_TEMPB", "TEMP_COIL_B_MAX", "TEMP_B_MAX", "WINDING_B_MAX", "COIL_B_MAX"]),
        new("windingCAllowed", "Nhiệt độ cho phép cuộn C", "threshold",
            ["SET_TEMPC", "TEMP_COIL_C_MAX", "TEMP_C_MAX", "WINDING_C_MAX", "COIL_C_MAX"]),
        new("bearingTopAllowed", "Nhiệt độ cho phép bi trên", "threshold",
            ["SET_TEMPNDE", "BEARING_TOP_MAX", "BEARING_UPPER_MAX", "O_BI_TREN_MAX"]),
        new("bearingBottomAllowed", "Nhiệt độ cho phép bi dưới", "threshold",
            ["SET_TEMPDE", "BEARING_BOTTOM_MAX", "BEARING_LOWER_MAX", "O_BI_DUOI_MAX"]),
    ];

    public static IReadOnlyList<SeriesDefinition> CurrentSeries { get; } =
    [
        // Excel Trend (MeterN/MetterN): I1=R, I2=S, I3=T
        new("phaseR", "Dòng điện R", "measured",
            ["I1", "CURRENT_L1", "PHASE_I1", "IA", "METER_I1", "I_R", "CURRENT_R"]),
        new("phaseS", "Dòng điện S", "measured",
            ["I2", "CURRENT_L2", "PHASE_I2", "IB", "METER_I2", "I_S", "CURRENT_S"]),
        new("phaseT", "Dòng điện T", "measured",
            ["I3", "CURRENT_L3", "PHASE_I3", "IC", "METER_I3", "I_T", "CURRENT_T"]),
        // Excel: Set_CurentR/S/T (typo Curent) — "dòng điện R/S/T cho phép"
        new("phaseRAllowed", "Dòng điện R cho phép", "threshold",
            ["SET_CURENTR", "SET_CURRENTR", "SET_I1", "I1_MAX", "CURRENT_L1_MAX", "CURRENT_R_MAX"]),
        new("phaseSAllowed", "Dòng điện S cho phép", "threshold",
            ["SET_CURENTS", "SET_CURRENTS", "SET_I2", "I2_MAX", "CURRENT_L2_MAX", "CURRENT_S_MAX"]),
        new("phaseTAllowed", "Dòng điện T cho phép", "threshold",
            ["SET_CURENTT", "SET_CURRENTT", "SET_I3", "I3_MAX", "CURRENT_L3_MAX", "CURRENT_T_MAX", "SET_CURRENT", "CURRENT_MAX"]),
    ];

    public static bool TryGetSeries(string chart, out IReadOnlyList<SeriesDefinition> series)
    {
        series = chart.Trim().ToLowerInvariant() switch
        {
            Temperature or "temp" or "nhiet" => TemperatureSeries,
            Current or "dong" or "ampere" => CurrentSeries,
            _ => Array.Empty<SeriesDefinition>()
        };
        return series.Count > 0;
    }

    public static bool Matches(SeriesDefinition def, string? raw) =>
        ElectricalParameterCatalog.Matches(
            new ElectricalParameterCatalog.Definition(def.Key, def.Label, null, def.Aliases),
            raw);
}
