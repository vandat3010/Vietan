namespace Backend.Application.DTOs.Scada;

public class StationReportColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public long? TagId { get; set; }
    public string? TagCode { get; set; }
}

public class StationReportRowDto
{
    public DateTimeOffset Time { get; set; }

    /// <summary>Giá trị theo <see cref="StationReportColumnDto.Key"/> (không gồm cột thời gian).</summary>
    public Dictionary<string, double?> Values { get; set; } = new(StringComparer.Ordinal);
}

public class StationReportTableDto
{
    public long StationId { get; set; }
    public long DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceCode { get; set; } = string.Empty;
    public string Interval { get; set; } = "30m";
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public IReadOnlyList<StationReportColumnDto> Columns { get; set; } = [];
    public IReadOnlyList<StationReportRowDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
