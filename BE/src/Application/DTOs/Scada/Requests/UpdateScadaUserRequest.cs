namespace Backend.Application.DTOs.Scada;

/// <summary>Cập nhật hồ sơ SCADA user — không đổi mật khẩu qua endpoint này.</summary>
public class UpdateScadaUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }

    /// <summary>viewer | Operator | Administrator / Admin</summary>
    public string? Role { get; set; }

    public int? Level { get; set; }

    public bool? IsActive { get; set; }

    public bool? MustChangePassword { get; set; }
}

/// <summary>Cập nhật metadata trạm.</summary>
public class UpdateStationRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
