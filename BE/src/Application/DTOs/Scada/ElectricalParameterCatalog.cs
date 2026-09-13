namespace Backend.Application.DTOs.Scada;

/// <summary>Catalog map tag thông số điện.</summary>
public static class ElectricalParameterCatalog
{
    public sealed record Definition(string Key, string Label, string? DefaultUnit, string[] Aliases);

    public static IReadOnlyList<Definition> Definitions { get; } =
    [
        new("voltageRs", "Điện áp dây RS", "V", ["U12", "VOLTAGE_RS", "U_RS", "URS", "V_RS", "DIEN_AP_RS"]),
        new("voltageSt", "Điện áp dây ST", "V", ["U23", "VOLTAGE_ST", "U_ST", "UST", "V_ST", "DIEN_AP_ST"]),
        new("voltageTr", "Điện áp dây TR", "V", ["U31", "VOLTAGE_TR", "U_TR", "UTR", "V_TR", "VOLTAGE_RT", "U_RT", "DIEN_AP_TR", "DIEN_AP_RT"]),
        new("currentA", "Dòng điện", "A", ["I_PH", "CURRENT", "CURRENT_A", "I_A", "DONG_DIEN"]),
        new("powerFactor", "Hệ số công suất", null, ["POWER_FACTOR", "PF", "COSPHI", "COS_PHI", "HS_CONG_SUAT"]),
        new("frequencyHz", "Tần số", "Hz", ["FREQUENCY", "FREQ", "TAN_SO"]),
        new("powerKw", "Công suất", "kW", ["TOTAL_KW", "POWER_KW", "CONG_SUAT"]),
        new("energyKwh", "Điện năng tiêu thụ", "kWh", ["ENERGY", "ENERGY_KWH", "KWH", "DIEN_NANG"]),
    ];

    public static bool Matches(Definition def, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var normalized = Normalize(raw);
        return def.Aliases.Any(a =>
        {
            var alias = Normalize(a);
            // Exact: CURRENT, VOLTAGE_RS
            // Prefixed per device: D1_VOLTAGE_RS, PUMP01_CURRENT
            return normalized == alias
                || (normalized.EndsWith("_" + alias, StringComparison.Ordinal)
                    // Tránh alias ngắn "POWER" khớp nhầm RATED_POWER / POWER_FACTOR
                    && !(alias == "POWER"
                         && (normalized.Contains("RATED_POWER", StringComparison.Ordinal)
                             || normalized.Contains("POWER_FACTOR", StringComparison.Ordinal)
                             || normalized.Contains("POWER_RATED", StringComparison.Ordinal))));
        });
    }

    private static string Normalize(string value) =>
        value.Trim().ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
}
