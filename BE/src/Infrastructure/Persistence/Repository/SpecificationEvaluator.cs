using Backend.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repository;

/// <summary>
/// Translates an <see cref="ISpecification{T}"/> into an IQueryable&lt;T&gt; against
/// EF Core. Kept in Infrastructure (not Domain/Application) because it is the only
/// piece that needs to know about EF Core's IQueryable/Include extension methods.
/// </summary>
public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> spec)
    {
        var query = inputQuery;

        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);

        return query;
    }
}
