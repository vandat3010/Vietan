using System.Data;
using Backend.Application.Interfaces.Dapper;
using Dapper;

namespace Backend.Infrastructure.Dapper.Repository;

/// <summary>
/// Generic Dapper implementation shared by every read-only/report query in the
/// system. Connections are opened and closed per call - short-lived by design.
/// </summary>
public class DapperRepository(IDapperContext dapperContext) : IDapperRepository
{
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        using var connection = dapperContext.CreateConnection();
        var command = new CommandDefinition(sql, parameters, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.QueryAsync<T>(command);
    }

    public async Task<T?> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        using var connection = dapperContext.CreateConnection();
        var command = new CommandDefinition(sql, parameters, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<T>(command);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        using var connection = dapperContext.CreateConnection();
        var command = new CommandDefinition(sql, parameters, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<T>(command);
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        using var connection = dapperContext.CreateConnection();
        var command = new CommandDefinition(sql, parameters, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(
        string storedProcedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = dapperContext.CreateConnection();
        var command = new CommandDefinition(
            storedProcedureName,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        return await connection.QueryAsync<T>(command);
    }

    public async Task<IEnumerable<TReturn>> QueryMultiMapAsync<TFirst, TSecond, TReturn>(
        string sql,
        Func<TFirst, TSecond, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CancellationToken cancellationToken = default)
    {
        using var connection = dapperContext.CreateConnection();
        return await connection.QueryAsync(sql, map, parameters, splitOn: splitOn);
    }
}
