namespace Backend.Application.DTOs.Scada;

/// <summary>Catalog tag nhiệt độ bơm (báo cáo history_30s).</summary>
public static class PumpTemperatureReportCatalog
{
    public sealed record Definition(string Key, string[] Aliases);

    public static IReadOnlyList<Definition> Definitions { get; } =
    [
        new("tempA", ["TEMP_COIL_A", "TEMP_A", "WINDING_A", "COIL_A", "NHIET_DO_A"]),
        new("tempB", ["TEMP_COIL_B", "TEMP_B", "WINDING_B", "COIL_B", "NHIET_DO_B"]),
        new("tempC", ["TEMP_COIL_C", "TEMP_C", "WINDING_C", "COIL_C", "NHIET_DO_C"]),
        new("bearingBottom", ["BEARING_BOTTOM", "BEARING_LOWER", "O_BI_DUOI", "BI_DUOI"]),
        new("bearingTop", ["BEARING_TOP", "BEARING_UPPER", "O_BI_TREN", "BI_TREN"]),
    ];

    public static bool Matches(Definition def, string? raw) =>
        ElectricalParameterCatalog.Matches(
            new ElectricalParameterCatalog.Definition(def.Key, def.Key, null, def.Aliases),
            raw);
}
