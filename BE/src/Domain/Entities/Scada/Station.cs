namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Pump station (top-level site). One station owns many PLCs.
/// Mapped to <c>scada.station</c>.
/// </summary>
public class Station : ScadaEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public ICollection<Plc> Plcs { get; set; } = new List<Plc>();
}
