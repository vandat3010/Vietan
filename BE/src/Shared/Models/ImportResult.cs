namespace Backend.Shared.Models;

/// <summary>
/// Outcome of parsing a spreadsheet into <typeparamref name="T"/> rows. Valid rows and
/// failures are reported together (rather than failing fast) because bulk imports are
/// almost always fixed iteratively: the user needs every bad cell in one pass, not the
/// first one. Lives in <c>Shared</c> so Api endpoints can return this shape directly.
/// </summary>
public class ImportResult<T>
{
    /// <summary>Rows that parsed cleanly. Nothing has been persisted - that is the caller's job.</summary>
    public IReadOnlyList<T> ValidRows { get; init; } = [];

    public IReadOnlyList<ImportError> Errors { get; init; } = [];

    /// <summary>Data rows seen in the sheet, including the ones that failed - so callers can report "X of Y imported".</summary>
    public int TotalRows { get; init; }

    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// A single cell/row failure, addressed the way the user sees it in Excel:
/// <c>RowNumber</c> is the 1-based worksheet row, not the index within
/// <see cref="ImportResult{T}.ValidRows"/>, so the message can be acted on directly.
/// </summary>
public sealed record ImportError(int RowNumber, string ColumnName, string Message);
