namespace Backend.Shared.Pagination;

public enum SortDirection
{
    Ascending = 0,
    Descending = 1
}

/// <summary>
/// One "field + direction" pair. A list of these is what enables multi-level
/// sorting ("Status ASC, CreatedDate DESC") without every list endpoint
/// inventing its own comma-separated string format.
/// </summary>
public class SortDescriptor(string field, SortDirection direction = SortDirection.Ascending)
{
    public string Field { get; set; } = field;
    public SortDirection Direction { get; set; } = direction;

    public SortDescriptor() : this(string.Empty)
    {
    }

    /// <summary>
    /// Parses the wire format used by clients: "CreatedDate desc" or "Name".
    /// Returns null for blank input so callers can just skip it.
    /// </summary>
    public static SortDescriptor? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var direction = parts.Length > 1 && parts[1].StartsWith("desc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Descending
            : SortDirection.Ascending;

        return new SortDescriptor(parts[0], direction);
    }
}
