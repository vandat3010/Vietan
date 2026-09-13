using Backend.Shared.Exceptions;

namespace Backend.Domain.Common;

/// <summary>
/// Thrown when an operation would violate a business invariant enforced by an
/// entity/aggregate/value object (e.g. deactivating an already-inactive user).
/// Extends <see cref="BusinessException"/> (from <c>Shared.Exceptions</c>) so the
/// Api's global exception middleware handles it via the same 400-mapping branch
/// as any other business rule violation, without Domain needing to reference
/// Application or Infrastructure - only the dependency-free <c>Shared</c> project.
/// </summary>
public class DomainException(string message) : BusinessException(message, "DomainRuleViolation");
