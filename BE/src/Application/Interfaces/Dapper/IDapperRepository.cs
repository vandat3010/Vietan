using System.Data;

namespace Backend.Application.Interfaces.Dapper;

/// <summary>
/// Generic, high-performance READ-ONLY data access built on top of Dapper.
/// Reserved for dashboards, reports, complex joins and stored-procedure calls -
/// never for Create/Update/Delete (those always go through the EF Core
/// repository + Unit of Work so change tracking/migrations stay authoritative).
/// </summary>
public interface IDapperRepository
{
    Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    Task<T?> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(
        string storedProcedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>Multi-mapping query for dashboard-style joins across several tables.</summary>
    Task<IEnumerable<TReturn>> QueryMultiMapAsync<TFirst, TSecond, TReturn>(
        string sql,
        Func<TFirst, TSecond, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CancellationToken cancellationToken = default);
}
