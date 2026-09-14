namespace Backend.Application.DTOs.Scada;

public class UpdateAppSettingRequest
{
    /// <summary>Raw setting value (validated against AppSettingCatalog).</summary>
    public string? SettingValue { get; set; }

    public bool? IsEnable { get; set; }
}

public class AppSettingCatalogItemDto
{
    public string Key { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool EditableViaApi { get; set; }
    public string? DefaultValue { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
}
