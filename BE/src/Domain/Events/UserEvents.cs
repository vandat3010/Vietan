using Backend.Domain.Common;

namespace Backend.Domain.Events;

public sealed class UserCreatedEvent(Guid userId, string email) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public string Email { get; } = email;
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed class UserPasswordChangedEvent(Guid userId) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed class UserDeactivatedEvent(Guid userId, string? reason) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public string? Reason { get; } = reason;
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed class UserLockedOutEvent(Guid userId) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
