using System.Data;
using Backend.Application.Interfaces.Dapper;
using Backend.Infrastructure.Common;

namespace Backend.Infrastructure.Dapper.Context;

/// <summary>
/// Application-facing entry point for the read-only/reporting side. Deliberately
/// stateless and connection-per-call (no shared/ambient transaction with EF Core) -
/// Dapper queries are meant to be fast, disposable, read-only round trips.
/// <para>
/// Connection creation itself is delegated to
/// <see cref="ISqlConnectionFactory"/> (Infrastructure/Common) so that Dapper is
/// not the only component able to obtain a raw connection, and so provider
/// details (Npgsql, connection string resolution) live in exactly one class.
/// </para>
/// </summary>
public class DapperContext(ISqlConnectionFactory connectionFactory) : IDapperContext
{
    public IDbConnection CreateConnection() => connectionFactory.CreateConnection();
}
