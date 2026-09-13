namespace Backend.Domain.Entities.Scada;

/// <summary>
/// SCADA operator account — ERD <c>Users</c> mapped to <c>scada.users</c>.
/// ERD columns: Username, PasswordHash, FullName, Email, Role, IsActive, timestamps.
/// Extra security columns (not on the ERD diagram) preserve lockout / must-change /
/// password-expiry / concurrent-session flows from recent requirements.
/// Distinct from IAM <c>app.Users</c>.
/// </summary>
public class ScadaUser : ScadaEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>ERD: FullName (was DisplayName).</summary>
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Role { get; set; } = string.Empty;

    /// <summary>ERD: IsActive (was IsEnable).</summary>
    public bool IsActive { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    // --- Profile extensions (beyond ERD; keep for Phase 0 APIs) ---

    public string? Unit { get; set; }

    public int? Level { get; set; }

    public string? Department { get; set; }

    public string? Position { get; set; }

    public string? Description { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    // --- Security extensions (beyond ERD; required by recent auth work) ---

    public bool MustChangePassword { get; set; }

    public DateTimeOffset? PasswordUpdatedAt { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTimeOffset? LockoutUntil { get; set; }
}
