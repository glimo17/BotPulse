using System;
using System.Linq;
using System.Threading.Tasks;
using BotPulse.Authorization.Entities;
using BotPulse.Authorization.Permissions;
using BotPulse.Authorization.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public RolesController(IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    /// <summary>List all roles with their permissions.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.RolesView)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var roles = await _roleRepository.GetAllAsync();
        return Ok(roles.Select(r => new RoleDto(r)));
    }

    /// <summary>Get a role by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.RolesView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        return Ok(new RoleDto(role));
    }

    /// <summary>Create a new custom role.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionCatalog.RolesUpdate)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        // Custom roles created via API are never system roles
        var role = new Role
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Permissions = request.Permissions?.ToList() ?? new(),
            IsSystemRole = false
        };

        await _roleRepository.CreateAsync(role);
        return CreatedAtAction(nameof(GetById), new { id = role.Id, version = "1" }, new RoleDto(role));
    }

    /// <summary>Update a custom role's name, description and permissions.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.RolesUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var existing = await _roleRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.IsSystemRole)
        {
            return Conflict(new { error = "System roles cannot be modified." });
        }

        existing.Name = request.Name;
        existing.Description = request.Description ?? existing.Description;
        existing.Permissions = request.Permissions?.ToList() ?? new();
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _roleRepository.UpdateAsync(existing);
        return Ok(new RoleDto(existing));
    }

    /// <summary>Delete a custom role. Blocked if system role or users are assigned.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.RolesUpdate)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _roleRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.IsSystemRole)
        {
            return Conflict(new { error = "System roles cannot be deleted." });
        }

        var hasUsers = await _userRoleRepository.RoleHasUsersAsync(id);
        if (hasUsers)
        {
            return Conflict(new { error = "Cannot delete a role that has users assigned. Reassign users first." });
        }

        await _roleRepository.DeleteAsync(id);
        return NoContent();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    string[] Permissions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public RoleDto(Role role)
        : this(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.Permissions.ToArray(),
            role.CreatedAtUtc,
            role.UpdatedAtUtc)
    {
    }
}

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    string[] Permissions);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    string[] Permissions);
