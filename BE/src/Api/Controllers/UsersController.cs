using Backend.Application.DTOs.Users;
using Backend.Application.Interfaces.Services;
using Backend.Domain.Enums;
using Backend.Shared.Constants;
using Backend.Shared.Helpers;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>Full CRUD sample built on top of the User aggregate (see Domain.Entities.User).</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsersController(IUserService userService, ISystemAuditService auditService) : ControllerBase
{
    /// <summary>
    /// Paged user list. Supports keyword search plus status/role/created-date
    /// filters and multi-field sorting, e.g.
    /// <c>?keyword=nguyen&amp;status=Active&amp;sortBy=CreatedDate&amp;sortDirection=Descending</c>.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Users.View)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] UserSearchQuery query, CancellationToken cancellationToken)
    {
        var result = await userService.GetAllAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginationResult<UserDto>>.Ok(result.Value!));
    }

    /// <summary>
    /// Xuất danh sách user ra .xlsx với cùng bộ lọc như <see cref="GetAll"/>,
    /// ví dụ <c>?keyword=nguyen&amp;status=Active</c>.
    /// </summary>
    [HttpGet("export")]
    [Authorize(Policy = Permissions.Users.View)]
    [Produces(ApplicationConstants.ExcelContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromQuery] UserSearchQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.ExportExcelAsync(query, cancellationToken);

            if (result.IsFailure)
            {
                await auditService.LogAsync(
                    BuildExportEntry(query, AuditStatus.Failed, "User export failed.", null), cancellationToken);
                return BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "User export failed.", result.Errors));
            }

            var bytes = result.Value!;
            await auditService.LogAsync(
                BuildExportEntry(query, AuditStatus.Success, "Exported users to Excel.", bytes.Length), cancellationToken);

            return File(
                bytes,
                ApplicationConstants.ExcelContentType,
                FileHelper.GenerateTimestampedFileName("Users", "xlsx"));
        }
        catch
        {
            // Record the business-level export failure; the global handler still maps
            // + audits the technical (System) error separately.
            await auditService.LogAsync(
                BuildExportEntry(query, AuditStatus.Failed, "User export failed (unexpected error).", null), CancellationToken.None);
            throw;
        }
    }

    // BE 3.1a — DataExport auditing is owned by the export endpoint (business context).
    private static SystemAuditEntry BuildExportEntry(UserSearchQuery query, AuditStatus status, string description, int? sizeBytes) =>
        new()
        {
            Action = AuditActionNames.Export,
            EventType = AuditEventType.DataExport,
            Status = status,
            Module = "Users",
            EntityType = "User",
            Description = description,
            AdditionalData = new Dictionary<string, object?>
            {
                ["format"] = "xlsx",
                ["keyword"] = query.Keyword,
                ["sizeBytes"] = sizeBytes
            }
        };

    /// <summary>Returns a single user by id.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Users.View)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await userService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<UserDto>.Ok(result.Value!))
            : NotFound(ApiResponse<UserDto>.Fail(result.Errors.FirstOrDefault() ?? "User not found."));
    }

    /// <summary>Creates a new user.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Users.Create)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto request, CancellationToken cancellationToken)
    {
        var result = await userService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ApiResponse<UserDto>.Ok(result.Value, "User created."))
            : Conflict(ApiResponse<UserDto>.Fail(result.Errors.FirstOrDefault() ?? "Failed to create user.", result.Errors));
    }

    /// <summary>Updates an existing user's profile.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto request, CancellationToken cancellationToken)
    {
        var result = await userService.UpdateAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<UserDto>.Ok(result.Value!, "User updated."))
            : NotFound(ApiResponse<UserDto>.Fail(result.Errors.FirstOrDefault() ?? "User not found."));
    }

    /// <summary>Soft-deletes a user.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Users.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await userService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse.Ok("User deleted."))
            : NotFound(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "User not found."));
    }

    /// <summary>Changes the authenticated user's own password, or (with permission) another user's.</summary>
    [HttpPost("{id:guid}/change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto request, CancellationToken cancellationToken)
    {
        var result = await userService.ChangePasswordAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Password changed."))
            : BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "Failed to change password.", result.Errors));
    }

    /// <summary>Replaces a user's role assignments.</summary>
    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesDto request, CancellationToken cancellationToken)
    {
        var result = await userService.AssignRolesAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<UserDto>.Ok(result.Value!, "Roles updated."))
            : BadRequest(ApiResponse<UserDto>.Fail(result.Errors.FirstOrDefault() ?? "Failed to update roles.", result.Errors));
    }

    /// <summary>Deactivates a user account.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(Guid id, [FromQuery] DeactivateUserQuery query, CancellationToken cancellationToken)
    {
        var result = await userService.DeactivateAsync(id, query.Reason, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse.Ok("User deactivated."))
            : BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "Failed to deactivate user.", result.Errors));
    }

    /// <summary>Reactivates a previously deactivated user account.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await userService.ActivateAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse.Ok("User activated."))
            : BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "Failed to activate user.", result.Errors));
    }
}
