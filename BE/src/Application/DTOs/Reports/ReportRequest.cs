namespace Backend.Application.DTOs.Reports;

/// <summary>
/// Describes a single read-only report execution for <c>IReportService</c>.
/// <para>
/// SECURITY: <see cref="Sql"/> and <see cref="StoredProcedureName"/> must always be
/// resolved server-side from a trusted report catalogue (a constants class such as
/// <c>UserSql</c>, a configuration file or a report-definition table). Never bind
/// either of them straight from an HTTP request body/query string - that would hand
/// callers arbitrary SQL execution against the reporting connection. Only
/// <see cref="Parameters"/> may originate from user input, and they are sent as
/// Dapper parameters so the values can never alter the shape of the statement.
/// </para>
/// </summary>
public class ReportRequest
{
    /// <summary>
    /// Raw SQL statement to run. Used only when <see cref="IsStoredProcedure"/> is
    /// <c>false</c>; exactly one of <see cref="Sql"/> / <see cref="StoredProcedureName"/>
    /// is ever read, the other is ignored.
    /// </summary>
    public string? Sql { get; init; }

    /// <summary>
    /// Stored procedure to call. Used only when <see cref="IsStoredProcedure"/> is
    /// <c>true</c>; exactly one of <see cref="Sql"/> / <see cref="StoredProcedureName"/>
    /// is ever read, the other is ignored.
    /// </summary>
    public string? StoredProcedureName { get; init; }

    /// <summary>
    /// Parameter name -> value. Passed to Dapper as command parameters; string
    /// concatenation into the statement is never acceptable, even for "trusted" values.
    /// </summary>
    public IDictionary<string, object?>? Parameters { get; init; }

    /// <summary>Selects which of the two statement properties above is executed.</summary>
    public bool IsStoredProcedure { get; init; }

    /// <summary>
    /// Per-report override for long-running analytical queries that legitimately
    /// exceed the provider default. <c>null</c> keeps the connection's own timeout.
    /// </summary>
    public int? CommandTimeoutSeconds { get; init; }
}
