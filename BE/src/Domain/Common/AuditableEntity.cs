namespace Backend.Domain.Common;

/// <summary>
/// Adds "who" on top of the "when" already provided by <see cref="BaseEntity{TId}"/>
/// (<see cref="BaseEntity{TId}.CreatedDate"/>/<see cref="BaseEntity{TId}.ModifiedDate"/>),
/// plus soft-delete via <see cref="ISoftDelete"/>. EF Core's SaveChangesAsync
/// override (see ApplicationDbContext) stamps all of these automatically using
/// <c>ICurrentUserService</c>/<c>IDateTimeProvider</c>, so individual entities
/// never set them by hand.
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity() : base(Guid.NewGuid())
    {
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }
}

public abstract class AuditableEntity<TId> : AggregateRoot<TId>, ISoftDelete where TId : notnull
{
    protected AuditableEntity(TId id) : base(id)
    {
    }

    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string? DeletedBy { get; set; }

    public void MarkAsDeleted(string? deletedBy)
    {
        IsDeleted = true;
        DeletedDate = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedDate = null;
        DeletedBy = null;
    }
}
