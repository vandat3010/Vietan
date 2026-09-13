namespace Backend.Application.Options;

/// <summary>Bound from "Realtime" in appsettings.</summary>
public class RealtimeOptions
{
    public const string SectionName = "Realtime";

    /// <summary><c>Fake</c> (default, no Redis server) or <c>Redis</c>.</summary>
    public string Provider { get; set; } = "Fake";

    public string RedisKeyPrefix { get; set; } = "scada:rt";

    public bool SimulateChanges { get; set; } = true;

    public int SimulateIntervalSeconds { get; set; } = 2;

    public bool SeedExcelOnStartup { get; set; } = true;

    /// <summary>Workbook used only to seed tag/device/screen mapping. Not a runtime database.</summary>
    public string ExcelPath { get; set; } = "data/Dinh_Nghia_Tag_Thuy_Loi_Ha_Noi.xlsx";
}
