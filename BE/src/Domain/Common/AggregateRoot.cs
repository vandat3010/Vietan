namespace Backend.Domain.Common;

/// <summary>
/// An Entity that is the single entry point for changes to a cluster of related
/// objects (the "aggregate"). Only aggregate roots are exposed through
/// <c>IRepository&lt;T&gt;</c> - child entities are only reachable/mutated through the
/// root's methods, which is what keeps invariants consistent.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot() : base(Guid.NewGuid())
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
}

public abstract class AggregateRoot<TId> : BaseEntity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
