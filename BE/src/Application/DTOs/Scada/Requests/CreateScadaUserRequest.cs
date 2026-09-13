namespace Backend.Application.DTOs.Scada;

/// <summary>Tạo SCADA user (màn Thêm người dùng mới) — không phát token.</summary>
public class CreateScadaUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ConfirmPassword { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }

    /// <summary>viewer | Operator | Administrator / Admin</summary>
    public string Role { get; set; } = "Operator";

    public int? Level { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Bắt buộc đổi mật khẩu lần đăng nhập đầu.</summary>
    public bool MustChangePassword { get; set; } = true;
}
