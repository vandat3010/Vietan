namespace Backend.Application.DTOs.Map;

public class MapLayerMetaDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public double Opacity { get; set; } = 1;
    public double Weight { get; set; } = 2;
    public bool Visible { get; set; } = true;
    public string Color { get; set; } = "#3388ff";
}

public class MapLayerDto
{
    public string Id { get; set; } = string.Empty;
    public MapLayerMetaDto Meta { get; set; } = new();
    public string FileUrl { get; set; } = string.Empty;
}

public class UpdateMapLayerRequest
{
    public string? Name { get; set; }
    public double? Opacity { get; set; }
    public double? Weight { get; set; }
    public bool? Visible { get; set; }
    public string? Color { get; set; }
}
