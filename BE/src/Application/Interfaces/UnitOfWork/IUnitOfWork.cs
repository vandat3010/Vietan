using Backend.Application.Interfaces.Repositories;

namespace Backend.Application.Interfaces.UnitOfWork;

/// <summary>
/// Coordinates one or more repositories that must be persisted together inside a
/// single EF Core transaction. Application services always depend on this
/// interface (never on ApplicationDbContext directly), which is what allows
/// swapping the persistence provider without touching business logic.
/// See the README "Transaction Flow" section for the full request lifecycle.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }

    /// <summary>Persists all pending changes tracked across every repository above.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicit transaction control for multi-step operations that must be
    /// all-or-nothing (e.g. "create user" + "assign default role" + "write audit log").
    /// SaveChangesAsync already wraps a single call in an implicit transaction,
    /// so this is only needed when you must call SaveChangesAsync more than once.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside begin/commit, rolling back on any
    /// exception. Prefer this over calling the three methods above by hand: a
    /// forgotten rollback in a catch block leaks an open transaction (and its row
    /// locks) for the rest of the request.
    /// <code>
    /// await unitOfWork.ExecuteInTransactionAsync(async ct =>
    /// {
    ///     await unitOfWork.Orders.CreateAsync(order, ct);
    ///     await unitOfWork.SaveChangesAsync(ct);
    ///     inventory.Reserve(order.Lines);
    ///     await unitOfWork.SaveChangesAsync(ct);
    /// }, cancellationToken);
    /// </code>
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExecuteInTransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}
