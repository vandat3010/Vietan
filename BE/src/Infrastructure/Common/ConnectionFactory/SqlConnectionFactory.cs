using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Backend.Infrastructure.Common;

/// <summary>
/// PostgreSQL/TimescaleDB implementation of <see cref="ISqlConnectionFactory"/>.
/// Resolves the connection string once at construction so callers never touch
/// <see cref="IConfiguration"/>, and hands out connection-per-call instances
/// (Npgsql pools the underlying physical connections).
/// </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
