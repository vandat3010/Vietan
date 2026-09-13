namespace Backend.Domain.Common;

/// <summary>
/// Marker for something that happened inside an aggregate that other parts of the
/// system might care about (e.g. UserCreatedEvent -> send welcome email).
/// Dispatched by <c>ApplicationDbContext.SaveChangesAsync</c> after a successful commit,
/// which keeps the Domain layer free of any messaging/eventing infrastructure.
/// No external mediator library is required: <see cref="AggregateRoot"/> simply
/// buffers events and the Infrastructure layer drains + publishes them.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
