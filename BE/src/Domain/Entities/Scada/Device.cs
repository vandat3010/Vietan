namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Physical device managed by a PLC (pump, meter, level sensor, …).
/// Mapped to <c>scada.device</c>.
/// </summary>
public class Device : ScadaEntity
{
    public long PlcId { get; set; }

    /// <summary>ERD FK to DeviceType (nullable until lookup rows are seeded).</summary>
    public int? DeviceTypeId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Legacy free-text type; prefer <see cref="DeviceTypeId"/>.</summary>
    public string DeviceType { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>ERD IsActive — column still <c>is_enable</c> until migration renames.</summary>
    public bool IsActive { get; set; }

    public Plc Plc { get; set; } = null!;

    public DeviceType? DeviceTypeNav { get; set; }

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
