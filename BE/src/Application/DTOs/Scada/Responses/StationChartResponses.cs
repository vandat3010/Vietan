namespace Backend.Application.DTOs.Scada;

public class StationChartDeviceOptionDto
{
    /// <summary>Device.Id — value của select / path chart history.</summary>
    public long Id { get; set; }

    /// <summary>Device.Name — label hiển thị select.</summary>
    public string Name { get; set; } = string.Empty;
}

public class StationChartHistoryQuery
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    /// <summary>1s | 30s | 1m | 15m (mặc định chart: đọc history_1s rồi gộp 15 phút).</summary>
    public string? Interval { get; set; }
}

public class StationChartHistoryDto
{
    public long StationId { get; set; }
    public long DeviceId { get; set; }
    public string Chart { get; set; } = string.Empty;
    public string Interval { get; set; } = "1s";
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public IReadOnlyList<StationChartSeriesDto> Series { get; set; } = [];
}

public class StationChartSeriesDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>measured | threshold</summary>
    public string Role { get; set; } = "measured";
    public long? TagId { get; set; }
    public string? TagCode { get; set; }
    public IReadOnlyList<StationChartPointDto> Points { get; set; } = [];
}

public class StationChartPointDto
{
    public DateTimeOffset Timestamp { get; set; }
    public double Value { get; set; }
}
