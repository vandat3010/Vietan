namespace Backend.Application.DTOs.Scada;

/// <summary>1 dòng bảng báo cáo mức nước (đúng cột UI).</summary>
public class WaterLevelReportRowDto
{
    public DateTimeOffset Time { get; set; }
    public double? RiverLevel { get; set; }
    public double? Discharge1 { get; set; }
    public double? Discharge2 { get; set; }
    public double? Discharge3 { get; set; }
    public double? Discharge4 { get; set; }
    public double? Discharge5 { get; set; }
    public double? Discharge6 { get; set; }
    public double? Discharge7 { get; set; }
    public double? Discharge8 { get; set; }
    public double? Discharge9 { get; set; }
    public double? Discharge10 { get; set; }
}
