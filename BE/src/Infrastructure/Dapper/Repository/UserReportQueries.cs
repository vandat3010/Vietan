using Backend.Application.DTOs.Reports;
using Backend.Application.Interfaces.Dapper;
using Backend.Infrastructure.Dapper.Sql;

namespace Backend.Infrastructure.Dapper.Repository;

/// <summary>
/// Concrete implementation of the "User dashboard" report contract. This is the
/// pattern every new report/dashboard should follow: a small class that only
/// depends on <see cref="IDapperRepository"/> and hand-written SQL/stored procedures.
/// </summary>
public class UserReportQueries(IDapperRepository dapperRepository) : IUserReportQueries
{
    public async Task<UserSummaryReportDto> GetUserSummaryAsync(CancellationToken cancellationToken = default)
    {
        var result = await dapperRepository.QuerySingleAsync<UserSummaryReportDto>(
            UserSql.GetUserSummary,
            cancellationToken: cancellationToken);

        return result ?? new UserSummaryReportDto();
    }

    public async Task<IEnumerable<UserActivityReportDto>> GetTopActiveUsersAsync(int top, CancellationToken cancellationToken = default) =>
        await dapperRepository.QueryAsync<UserActivityReportDto>(
            UserSql.GetTopActiveUsers,
            new { Top = top },
            cancellationToken: cancellationToken);
}
