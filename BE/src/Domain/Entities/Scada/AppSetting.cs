namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Key/value application settings for the SCADA stack.
/// Mapped to <c>scada.app_settings</c>.
/// </summary>
public class AppSetting : ScadaEntity
{
    public string SettingKey { get; set; } = string.Empty;

    public string? SettingValue { get; set; }

    public string DataType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnable { get; set; }
}
