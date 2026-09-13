namespace Backend.Application.DTOs.Reports;

/// <summary>Sample dashboard/report DTOs, populated via Dapper (see IUserReportQueries).</summary>
public class UserSummaryReportDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int NewUsersLast30Days { get; set; }
}

public class UserActivityReportDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public DateTime? LastLoginAtUtc { get; set; }
    public int LoginCountLast30Days { get; set; }
}
