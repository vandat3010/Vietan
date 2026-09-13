using Backend.Application.DTOs.Users;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PaginationResult<UserDto>>> GetAllAsync(UserSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất Excel danh sách user theo đúng bộ lọc của
    /// <see cref="GetAllAsync"/>, nhưng lấy toàn bộ kết quả khớp chứ không chỉ
    /// một trang - người dùng bấm "Xuất Excel" là muốn cả danh sách đang lọc.
    /// </summary>
    Task<Result<byte[]>> ExportExcelAsync(UserSearchQuery query, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateAsync(CreateUserDto request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid id, ChangePasswordDto request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> AssignRolesAsync(Guid id, AssignRolesDto request, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(Guid id, string? reason, CancellationToken cancellationToken = default);
    Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
}
