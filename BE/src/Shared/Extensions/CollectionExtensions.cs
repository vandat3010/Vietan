namespace Backend.Shared.Extensions;

/// <summary>Reusable <see cref="IEnumerable{T}"/>/collection helpers used across every layer.</summary>
public static class CollectionExtensions
{
    /// <summary>True when the sequence is null or has no elements.</summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => source is null || !source.Any();

    /// <summary>
    /// Splits a sequence into batches of at most <paramref name="size"/> items -
    /// useful for bulk inserts, batched API calls, or paging background jobs.
    /// <para>
    /// NOTE: this has the exact same signature as the BCL's <see cref="Enumerable.Chunk{TSource}"/>
    /// (.NET 6+). Because explicit <c>using</c> directives are resolved before
    /// implicit/global ones, a file with <c>using Backend.Shared.Extensions;</c> will
    /// bind to THIS overload (which simply delegates to the BCL after validating
    /// <paramref name="size"/>) with no ambiguity for the common case. Its only
    /// purpose is the friendlier <see cref="ArgumentOutOfRangeException"/> message -
    /// prefer calling <see cref="Enumerable.Chunk{TSource}"/> directly when this
    /// project isn't already imported.
    /// </para>
    /// </summary>
    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Chunk size must be greater than zero.");
        return Enumerable.Chunk(source, size);
    }
}
