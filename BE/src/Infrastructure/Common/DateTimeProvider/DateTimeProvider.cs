using Backend.Application.Common;

namespace Backend.Infrastructure.Common;

/// <summary>
/// The single place in the solution allowed to read the system clock, so that
/// every other class depends on <see cref="IDateTimeProvider"/> instead of
/// calling <c>DateTime.UtcNow</c> directly (which makes time-dependent logic
/// impossible to unit test deterministically).
/// <para>
/// NOTE: files under Infrastructure/Common use the flat
/// <c>Backend.Infrastructure.Common</c> namespace rather than one segment per
/// sub-folder - a <c>...Common.DateTimeProvider.DateTimeProvider</c> type name
/// would collide with its own namespace and force ugly full qualification at
/// every registration/usage site.
/// </para>
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
