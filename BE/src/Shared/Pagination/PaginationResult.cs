namespace Backend.Shared.Pagination;

/// <summary>
/// A page of <typeparamref name="T"/> plus the metadata a client needs to render
/// pagination controls, without leaking IQueryable/EF concerns out of Infrastructure.
/// <para>
/// This is the single paged-result type in the solution: "PagedResponse&lt;T&gt;" and
/// "PaginationResult&lt;T&gt;" describe the exact same data, so keeping two classes
/// would only create a mapping step with no benefit.
/// </para>
/// </summary>
public class PaginationResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginationResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PaginationResult<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize) =>
        new(items, totalCount, pageNumber, pageSize);

    public static PaginationResult<T> Create(IReadOnlyList<T> items, int totalCount, PaginationRequest request) =>
        new(items, totalCount, request.PageNumber, request.PageSize);

    public static PaginationResult<T> Empty(PaginationRequest request) =>
        new([], 0, request.PageNumber, request.PageSize);

    /// <summary>Projects the items of an existing page (entities -> DTOs) while keeping the metadata intact.</summary>
    public PaginationResult<TResult> Map<TResult>(Func<T, TResult> selector) =>
        new([.. Items.Select(selector)], TotalCount, PageNumber, PageSize);
}
