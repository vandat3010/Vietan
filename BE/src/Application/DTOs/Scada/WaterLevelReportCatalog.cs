namespace Backend.Application.DTOs.Scada;

/// <summary>Catalog tag báo cáo mức nước (sông + mức xả 1..10).</summary>
public static class WaterLevelReportCatalog
{
    public sealed record Definition(string Key, string[] Aliases);

    public static IReadOnlyList<Definition> Definitions { get; } =
    [
        new("riverLevel", ["LEVEL_RIVER", "WATER_RIVER", "MUC_NUOC_SONG"]),
        new("discharge1", ["LEVEL_DISCHARGE_1", "LEVEL_XA_1", "MUC_XA_1", "DISCHARGE_1"]),
        new("discharge2", ["LEVEL_DISCHARGE_2", "LEVEL_XA_2", "MUC_XA_2", "DISCHARGE_2"]),
        new("discharge3", ["LEVEL_DISCHARGE_3", "LEVEL_XA_3", "MUC_XA_3", "DISCHARGE_3"]),
        new("discharge4", ["LEVEL_DISCHARGE_4", "LEVEL_XA_4", "MUC_XA_4", "DISCHARGE_4"]),
        new("discharge5", ["LEVEL_DISCHARGE_5", "LEVEL_XA_5", "MUC_XA_5", "DISCHARGE_5"]),
        new("discharge6", ["LEVEL_DISCHARGE_6", "LEVEL_XA_6", "MUC_XA_6", "DISCHARGE_6"]),
        new("discharge7", ["LEVEL_DISCHARGE_7", "LEVEL_XA_7", "MUC_XA_7", "DISCHARGE_7"]),
        new("discharge8", ["LEVEL_DISCHARGE_8", "LEVEL_XA_8", "MUC_XA_8", "DISCHARGE_8"]),
        new("discharge9", ["LEVEL_DISCHARGE_9", "LEVEL_XA_9", "MUC_XA_9", "DISCHARGE_9"]),
        new("discharge10", ["LEVEL_DISCHARGE_10", "LEVEL_XA_10", "MUC_XA_10", "DISCHARGE_10"]),
    ];

    public static bool Matches(Definition def, string? raw) =>
        ElectricalParameterCatalog.Matches(
            new ElectricalParameterCatalog.Definition(def.Key, def.Key, null, def.Aliases),
            raw);
}
