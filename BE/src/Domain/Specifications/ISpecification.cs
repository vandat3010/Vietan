using System.Linq.Expressions;

namespace Backend.Domain.Specifications;

/// <summary>
/// Encapsulates a query's shape (filter, includes, ordering, paging) as a single
/// reusable, testable object instead of scattering LINQ across repositories/services.
/// The Specification Pattern keeps the Domain in control of "what makes a valid
/// query" while the Infrastructure repository only knows how to *execute* one.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    int Skip { get; }
    int Take { get; }
    bool IsPagingEnabled { get; }
}
