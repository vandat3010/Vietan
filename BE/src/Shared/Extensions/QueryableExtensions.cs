using System.Linq.Expressions;
using System.Reflection;
using Backend.Shared.Pagination;

namespace Backend.Shared.Extensions;

/// <summary>
/// Reusable <see cref="IQueryable{T}"/> helpers for paging/sorting so repositories
/// don't hand-roll <c>Skip/Take/OrderBy</c> (and its reflection-based dynamic
/// property lookup) for every list endpoint. All of these compose into the
/// provider's expression tree, so the work still happens in SQL - never in memory.
/// </summary>
public static class QueryableExtensions
{
    private const BindingFlags PropertyLookupFlags =
        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return query.Skip(request.Skip).Take(request.PageSize);
    }

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int pageNumber, int pageSize) =>
        query.Skip((Math.Max(pageNumber, 1) - 1) * pageSize).Take(pageSize);

    /// <summary>
    /// Orders by a property name known only at runtime (e.g. a "sortBy" query
    /// string parameter), avoiding a giant switch statement per entity. Unknown
    /// or blank property names are ignored rather than throwing, so a stale
    /// client cannot break a list endpoint with a bad sort field.
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? propertyName, SortDirection direction = SortDirection.Ascending)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return query;

        var property = typeof(T).GetProperty(propertyName, PropertyLookupFlags);
        if (property is null) return query;

        return ApplyOrder(query, property, direction, isFirstSort: true);
    }

    /// <summary>
    /// Multi-level sorting ("Status ASC, CreatedDate DESC"): the first valid
    /// descriptor becomes OrderBy, every subsequent one becomes ThenBy.
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, IReadOnlyList<SortDescriptor>? sorts)
    {
        if (sorts is null || sorts.Count == 0) return query;

        var isFirstSort = true;

        foreach (var sort in sorts)
        {
            if (string.IsNullOrWhiteSpace(sort.Field)) continue;

            var property = typeof(T).GetProperty(sort.Field, PropertyLookupFlags);
            if (property is null) continue;

            query = ApplyOrder(query, property, sort.Direction, isFirstSort);
            isFirstSort = false;
        }

        return query;
    }

    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, PaginationRequest request) =>
        query.ApplySorting(request.GetSortDescriptors());

    private static IQueryable<T> ApplyOrder<T>(IQueryable<T> query, PropertyInfo property, SortDirection direction, bool isFirstSort)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var keySelector = Expression.Lambda(propertyAccess, parameter);

        var methodName = (isFirstSort, direction) switch
        {
            (true, SortDirection.Ascending) => nameof(Queryable.OrderBy),
            (true, SortDirection.Descending) => nameof(Queryable.OrderByDescending),
            (false, SortDirection.Ascending) => nameof(Queryable.ThenBy),
            _ => nameof(Queryable.ThenByDescending)
        };

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), property.PropertyType],
            query.Expression,
            Expression.Quote(keySelector));

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}
