using Backend.Application.DTOs.Roles;
using Backend.Application.Interfaces.Services;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>Role management, backing the User &lt;-&gt; Role assignment endpoints.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.Roles.View)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await roleService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(result.Value!));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.View)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await roleService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<RoleDto>.Ok(result.Value!))
            : NotFound(ApiResponse<RoleDto>.Fail(result.Errors.FirstOrDefault() ?? "Role not found."));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto request, CancellationToken cancellationToken)
    {
        var result = await roleService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ApiResponse<RoleDto>.Ok(result.Value, "Role created."))
            : Conflict(ApiResponse<RoleDto>.Fail(result.Errors.FirstOrDefault() ?? "Failed to create role.", result.Errors));
    }
}
