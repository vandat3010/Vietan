namespace Backend.Domain.Common;

/// <summary>
/// Root type for every entity in the model. Identity-based equality (two entities
/// are equal when their Ids are equal, regardless of other property values) is
/// what distinguishes an Entity from a <see cref="ValueObject"/> in DDD.
/// <para>
/// Also carries the two timestamps EVERY entity needs regardless of whether it
/// requires full "who did it" auditing - <see cref="CreatedDate"/> and
/// <see cref="ModifiedDate"/> are stamped automatically by
/// <c>ApplicationDbContext.SaveChangesAsync</c> for every tracked entity.
/// Entities that also need "who" (not just "when") should derive from
/// <see cref="AuditableEntity{TId}"/> instead.
/// </para>
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() : base(Guid.NewGuid())
    {
    }

    protected BaseEntity(Guid id) : base(id)
    {
    }
}

public abstract class BaseEntity<TId> where TId : notnull
{
    public TId Id { get; protected set; }

    /// <summary>UTC timestamp of when the row was first inserted.</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>UTC timestamp of the most recent update, null until the first update.</summary>
    public DateTime? ModifiedDate { get; set; }

    protected BaseEntity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id.Equals(other.Id);
    }

    public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right) => !(left == right);

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
}
