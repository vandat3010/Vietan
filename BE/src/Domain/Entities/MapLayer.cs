using Backend.Domain.Common;

namespace Backend.Domain.Entities;

/// <summary>
/// Map overlay metadata (KMZ/KML). File bytes live in <c>IFileStorageService</c>.
/// Table: <c>app.map_layers</c>.
/// </summary>
public class MapLayer : AuditableEntity
{
    public MapLayer()
    {
    }

    public MapLayer(Guid id) : base(id)
    {
    }

    public string Name { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// <summary>Relative storage path returned by <c>IFileStorageService</c>.</summary>
    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public double Opacity { get; set; } = 1;

    public double Weight { get; set; } = 2;

    public bool Visible { get; set; } = true;

    public string Color { get; set; } = "#3388ff";

    public int SortOrder { get; set; }
}
