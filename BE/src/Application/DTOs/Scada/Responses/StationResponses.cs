namespace Backend.Application.DTOs.Scada;

public class StationDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>Chi tiết 1 trạm theo id — đủ cột scada.station dùng cho API.</summary>
public class StationDetailDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
