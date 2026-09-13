using Backend.Domain.Enums;

namespace Backend.Application.DTOs.Scada;

public sealed class RealtimeTagDto
{
    public long TagId { get; init; }
    public long DeviceId { get; init; }
    public long StationId { get; init; }
    public string TagCode { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string? Unit { get; init; }
    public string? Description { get; init; }
    public string? MappingLabel { get; init; }
    public object? Value { get; init; }
    public string Quality { get; init; } = "Good";
    public DateTimeOffset Timestamp { get; init; }
    public bool EnableAlarm { get; init; }
}

public sealed class RealtimeSnapshotDto
{
    public long StationId { get; init; }
    public long? DeviceId { get; init; }
    public ScadaScreenType ScreenType { get; init; }
    public string Screen { get; init; } = string.Empty;
    public bool IsRealtime { get; init; }
    public IReadOnlyList<RealtimeTagDto> Tags { get; init; } = [];
}

public sealed class ScreenTagQuery
{
    public long StationId { get; set; }
    public long? DeviceId { get; set; }
}

public sealed class ScreenHistoryQuery
{
    public long StationId { get; set; }
    public long? DeviceId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    /// <summary>1s | 30s | 1m | 30m. Default 30s.</summary>
    public string? Interval { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

public sealed class ScreenHistoryPointDto
{
    public DateTimeOffset Time { get; init; }
    public long TagId { get; init; }
    public string TagCode { get; init; } = string.Empty;
    public string? MappingLabel { get; init; }
    public double Value { get; init; }
}
