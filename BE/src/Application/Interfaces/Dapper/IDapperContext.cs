using System.Data;

namespace Backend.Application.Interfaces.Dapper;

/// <summary>
/// Thin factory for raw ADO.NET connections used by the Dapper read-side.
/// Deliberately NOT the same abstraction as ApplicationDbContext: Dapper
/// connections are short-lived and opened per-query, they never participate
/// in EF Core's change tracker or unit-of-work transaction.
/// </summary>
public interface IDapperContext
{
    IDbConnection CreateConnection();
}
