using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

public class ScadaUserQuery : PaginationRequest
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    /// <summary>Alias — prefer <see cref="IsActive"/>.</summary>
    public bool? IsEnable { get => IsActive; set => IsActive = value; }
}
