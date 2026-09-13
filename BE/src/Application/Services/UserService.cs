using Backend.Application.DTOs.Users;
using Backend.Application.Interfaces.Services;
using Backend.Application.Interfaces.UnitOfWork;
using Backend.Application.Mapping;
using Backend.Domain.Entities;
using Backend.Domain.Interfaces;
using Backend.Domain.ValueObjects;
using Backend.Shared.Constants;
using Backend.Shared.Models;
using Backend.Shared.Pagination;
using Backend.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Backend.Application.Services;

/// <summary>
/// Orchestrates the User aggregate: loads it (and related aggregates) through the
/// Unit of Work, invokes the rich domain methods to apply business rules, then
/// asks the Unit of Work to persist the result in a single transaction.
/// This service intentionally contains almost no business logic itself -
/// that lives on the <see cref="User"/> entity (see Domain layer).
/// </summary>
public class UserService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IImportExportService importExportService,
    ILogger<UserService> logger) : IUserService
{
    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetWithRolesAsync(id, cancellationToken);
        return user is null
            ? Result<UserDto>.Failure("User.NotFound", $"User '{id}' was not found.")
            : Result<UserDto>.Success(user.ToDto());
    }

    /// <remarks>
    /// Filtering, sorting and paging are all pushed down to the repository (and
    /// therefore to SQL). The previous in-memory version loaded every user on each
    /// request, which is fine with 50 users and fatal with 50,000.
    /// </remarks>
    public async Task<Result<PaginationResult<UserDto>>> GetAllAsync(UserSearchQuery query, CancellationToken cancellationToken = default)
    {
        var page = await unitOfWork.Users.SearchAsync(query, cancellationToken);
        return Result<PaginationResult<UserDto>>.Success(page.Map(user => user.ToDto()));
    }

    /// <summary>
    /// Ví dụ mẫu cho cách export theo cột tự khai báo: tiêu đề tiếng Việt, cột
    /// "Họ tên" ghép từ hai trường, cột ngày có định dạng riêng.
    /// </summary>
    public async Task<Result<byte[]>> ExportExcelAsync(UserSearchQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await LoadAllForExportAsync(query, cancellationToken);

        var columns = new ExcelColumns<UserDto>
        {
            { "Email", user => user.Email },
            { "Họ tên", user => $"{user.FirstName} {user.LastName}".Trim() },
            { "Số điện thoại", user => user.PhoneNumber },
            { "Trạng thái", user => user.Status },
            { "Vai trò", user => string.Join(", ", user.Roles) },
            { "Đăng nhập gần nhất", user => user.LastLoginAtUtc, ApplicationConstants.ExcelDateTimeFormat },
            { "Ngày tạo", user => user.CreatedDate, ApplicationConstants.ExcelDateTimeFormat }
        };

        var file = await importExportService.ExportExcelAsync(rows, columns, "Users", cancellationToken);

        logger.LogInformation("Exported {RowCount} user(s) to Excel", rows.Count);

        return Result<byte[]>.Success(file);
    }

    /// <summary>
    /// Đọc toàn bộ kết quả khớp bộ lọc theo từng trang thay vì một câu query
    /// khổng lồ, và dừng ở <see cref="ApplicationConstants.MaxExportRows"/> để
    /// một bộ lọc rỗng trên bảng lớn không kéo sập tiến trình.
    /// </summary>
    private async Task<IReadOnlyList<UserDto>> LoadAllForExportAsync(UserSearchQuery query, CancellationToken cancellationToken)
    {
        query.PageNumber = ApplicationConstants.DefaultPageNumber;
        query.PageSize = ApplicationConstants.MaxPageSize;

        var rows = new List<UserDto>();

        while (rows.Count < ApplicationConstants.MaxExportRows)
        {
            var page = await unitOfWork.Users.SearchAsync(query, cancellationToken);
            if (page.Items.Count == 0) break;

            rows.AddRange(page.Items.Select(user => user.ToDto()));

            if (rows.Count >= page.TotalCount) break;

            query.PageNumber++;
        }

        return rows;
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto request, CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
            return Result<UserDto>.Failure("User.EmailAlreadyExists", $"Email '{request.Email}' is already registered.");

        var email = Email.Create(request.Email);
        var fullName = FullName.Create(request.FirstName, request.LastName);
        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(email, fullName, passwordHash);
        user.UpdateProfile(fullName, request.PhoneNumber);

        foreach (var roleId in request.RoleIds)
            user.AssignRole(roleId);

        await unitOfWork.Users.CreateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} created with email {Email}", user.Id, user.Email.Value);

        var created = await unitOfWork.Users.GetWithRolesAsync(user.Id, cancellationToken);
        return Result<UserDto>.Success(created!.ToDto());
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserDto request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetWithRolesAsync(id, cancellationToken);
        if (user is null)
            return Result<UserDto>.Failure("User.NotFound", $"User '{id}' was not found.");

        var fullName = FullName.Create(request.FirstName, request.LastName);
        user.UpdateProfile(fullName, request.PhoneNumber);

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Result.Failure("User.NotFound", $"User '{id}' was not found.");

        unitOfWork.Users.Delete(user); // soft-deleted via SaveChanges interception, see ApplicationDbContext
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid id, ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Result.Failure("User.NotFound", $"User '{id}' was not found.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("User.InvalidCurrentPassword", "Current password is incorrect.");

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<UserDto>> AssignRolesAsync(Guid id, AssignRolesDto request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetWithRolesAsync(id, cancellationToken);
        if (user is null)
            return Result<UserDto>.Failure("User.NotFound", $"User '{id}' was not found.");

        var roles = await unitOfWork.Roles.GetByIdsAsync(request.RoleIds, cancellationToken);
        if (roles.Count != request.RoleIds.Distinct().Count())
            return Result<UserDto>.Failure("Role.NotFound", "One or more role ids do not exist.");

        foreach (var roleId in user.UserRoles.Select(ur => ur.RoleId).ToList())
            user.RemoveRole(roleId);

        foreach (var roleId in request.RoleIds)
            user.AssignRole(roleId);

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await unitOfWork.Users.GetWithRolesAsync(id, cancellationToken);
        return Result<UserDto>.Success(refreshed!.ToDto());
    }

    public async Task<Result> DeactivateAsync(Guid id, string? reason, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Result.Failure("User.NotFound", $"User '{id}' was not found.");

        user.Deactivate(reason);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Result.Failure("User.NotFound", $"User '{id}' was not found.");

        user.Activate();
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
