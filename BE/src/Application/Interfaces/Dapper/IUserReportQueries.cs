using Backend.Application.DTOs.Reports;

namespace Backend.Application.Interfaces.Dapper;

/// <summary>
/// Sample of a purpose-built, Dapper-backed "report/dashboard" query interface.
/// This is the pattern to follow for ERP/MES/SCADA dashboards: one interface per
/// bounded report, implemented against a hand-tuned SQL statement or stored
/// procedure, kept entirely separate from the EF Core write-side repositories.
/// </summary>
public interface IUserReportQueries
{
    Task<UserSummaryReportDto> GetUserSummaryAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserActivityReportDto>> GetTopActiveUsersAsync(int top, CancellationToken cancellationToken = default);
}
