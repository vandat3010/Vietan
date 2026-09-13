namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Base for SCADA metadata rows in schema <c>scada</c>.
/// Deliberately separate from <c>BaseEntity&lt;Guid&gt;</c> (IAM / app schema):
/// SCADA uses BIGSERIAL identity and TIMESTAMPTZ <c>created_at</c>/<c>updated_at</c>.
/// </summary>
public abstract class ScadaEntity
{
    public long Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
