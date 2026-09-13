using System.Data;

namespace Backend.Infrastructure.Common;

/// <summary>
/// Lowest-level abstraction over "give me a raw ADO.NET connection to the primary
/// database". Infrastructure-internal on purpose: the Application layer never sees
/// it (it depends on <c>IDapperContext</c>/repositories instead), so no
/// Clean Architecture boundary is crossed.
/// <para>
/// Used by the Dapper read-side (<c>DapperContext</c>) and available to any other
/// Infrastructure component needing a raw connection (health checks, TimescaleDB
/// bulk COPY ingestion, maintenance jobs) without each one re-reading connection
/// strings from configuration.
/// </para>
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>Creates a new, CLOSED connection. The caller owns and disposes it.</summary>
    IDbConnection CreateConnection();

    /// <summary>Creates and opens a new connection. The caller owns and disposes it.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
