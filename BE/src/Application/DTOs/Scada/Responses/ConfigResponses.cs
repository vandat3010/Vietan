namespace Backend.Application.DTOs.Scada;

public class HistoryProfileDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int IntervalSecond { get; set; }
    public int RetentionDay { get; set; }
    public int? CompressionDay { get; set; }
    public string? Description { get; set; }
    public bool IsEnable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class TagHistoryConfigDto
{
    public long Id { get; set; }
    public long TagId { get; set; }
    public long HistoryProfileId { get; set; }
    public string? TagCode { get; set; }
    public string? HistoryProfileCode { get; set; }
    public double? Deadband { get; set; }
    public int Priority { get; set; }
    public string? Description { get; set; }
    public bool IsEnable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CommunicationConfigDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class MqttConfigDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Broker { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? ClientId { get; set; }
    public string? TopicPublish { get; set; }
    public string? TopicSubscribe { get; set; }
    public int KeepAlive { get; set; }
    public short Qos { get; set; }
    public bool Retain { get; set; }
    public bool UseTls { get; set; }
    public bool IsEnable { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>SCADA operator — password hash không trả về.</summary>
public class ScadaUserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    /// <summary>ERD FullName.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Alias for FE compatibility.</summary>
    public string DisplayName { get => FullName; set => FullName = value; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? Level { get; set; }
    /// <summary>ERD IsActive.</summary>
    public bool IsActive { get; set; }
    /// <summary>Alias for FE compatibility.</summary>
    public bool IsEnable { get => IsActive; set => IsActive = value; }
    /// <summary>Nhãn trạng thái UI (VN).</summary>
    public string Status { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class AppSettingDto
{
    public long Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
