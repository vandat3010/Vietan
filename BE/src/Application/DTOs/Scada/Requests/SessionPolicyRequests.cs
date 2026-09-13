namespace Backend.Application.DTOs.Scada;

public class SessionPolicyDto
{
    public string SettingKey { get; set; } = string.Empty;
    public int IdleTimeoutMinutes { get; set; }
    public bool IsDefault { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class UpdateSessionPolicyRequest
{
    /// <summary>Số phút idle trước khi đóng phiên (1..10080).</summary>
    public int IdleTimeoutMinutes { get; set; }
}
