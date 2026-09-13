using Backend.Application.Interfaces.Repositories;
using Backend.Application.Interfaces.UnitOfWork;
using Backend.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// Wraps a single, request-scoped <see cref="ApplicationDbContext"/> so that every
/// repository resolved for this request shares the same change tracker and the
/// same transaction. See README "Transaction Flow" for the end-to-end diagram.
/// </summary>
public class UnitOfWork(
    ApplicationDbContext context,
    IUserRepository users,
    IRoleRepository roles) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public IUserRepository Users { get; } = users;
    public IRoleRepository Roles { get; } = roles;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction ??= await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) return;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) return;

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Joining an outer transaction instead of nesting: EF Core does not
        // support nested transactions, and silently committing an inner one would
        // break the all-or-nothing guarantee the caller is relying on.
        if (_currentTransaction is not null) return await operation(cancellationToken);

        await BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
            await _currentTransaction.DisposeAsync();

        await context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
