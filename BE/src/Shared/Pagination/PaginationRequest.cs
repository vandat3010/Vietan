using Backend.Shared.Constants;

namespace Backend.Shared.Pagination;

/// <summary>
/// Common query-string parameters accepted by every "list" endpoint
/// (<c>?pageNumber=1&amp;pageSize=20&amp;keyword=abc&amp;sortBy=CreatedDate&amp;sortDirection=Descending</c>).
/// Bind this directly as an action parameter (<c>[FromQuery] PaginationRequest request</c>)
/// instead of redeclaring the same five properties on every list DTO.
/// </summary>
public class PaginationRequest
{
    private int _pageNumber = ApplicationConstants.DefaultPageNumber;
    private int _pageSize = ApplicationConstants.DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? ApplicationConstants.DefaultPageNumber : value;
    }

    /// <summary>Clamped to <see cref="ApplicationConstants.MaxPageSize"/> so a client cannot ask for the whole table.</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            <= 0 => ApplicationConstants.DefaultPageSize,
            > ApplicationConstants.MaxPageSize => ApplicationConstants.MaxPageSize,
            _ => value
        };
    }

    /// <summary>Free-text search term; which columns it matches is decided by the repository/service.</summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Equality filters as "field -> value" (e.g. <c>status=Active</c>, <c>code=WH01</c>).
    /// Deliberately a plain dictionary rather than an expression/OData-style parser:
    /// each repository decides which keys it honours, so an unknown key can never
    /// turn into an unexpected (or unindexed) SQL predicate.
    /// </summary>
    public IDictionary<string, string>? Filters { get; set; }

    /// <summary>Primary sort field - see <see cref="Extensions.QueryableExtensions.ApplySorting{T}(IQueryable{T}, string?, SortDirection)"/>.</summary>
    public string? SortBy { get; set; }

    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Optional multi-level sort ("Status ASC, CreatedDate DESC"). When empty,
    /// <see cref="SortBy"/>/<see cref="SortDirection"/> are used instead.
    /// </summary>
    public IList<SortDescriptor>? Sorts { get; set; }

    /// <summary>Normalises the two sorting inputs above into a single list for repositories.</summary>
    public IReadOnlyList<SortDescriptor> GetSortDescriptors()
    {
        if (Sorts is { Count: > 0 }) return [.. Sorts];
        return string.IsNullOrWhiteSpace(SortBy) ? [] : [new SortDescriptor(SortBy, SortDirection)];
    }

    public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);

    public int Skip => (PageNumber - 1) * PageSize;
}
