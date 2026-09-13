namespace Backend.Application.Common;

/// <summary>
/// Wraps <see cref="DateTime.UtcNow"/> so Application services stay unit-testable
/// (tests can inject a fixed clock instead of relying on the real system time).
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
