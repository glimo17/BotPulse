using System;
using System.Linq;
using System.Threading.Tasks;
using BotPulse.Authorization.Entities;
using BotPulse.Authorization.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "RequireAdministrator")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;

    public RolesController(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var roles = await _roleRepository.GetAllAsync();
        return Ok(roles.Select(r => new RoleDto(r)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        return Ok(new RoleDto(role));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var role = new Role
        {
            Name = request.Name,
            Permissions = request.Permissions?.ToList() ?? new(),
            IsSystemRole = request.IsSystemRole
        };

        await _roleRepository.CreateAsync(role);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, new RoleDto(role));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var existing = await _roleRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = request.Name;
        existing.Permissions = request.Permissions?.ToList() ?? new();
        existing.IsSystemRole = request.IsSystemRole;

        await _roleRepository.UpdateAsync(existing);
        return Ok(new RoleDto(existing));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _roleRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _roleRepository.DeleteAsync(id);
        return NoContent();
    }
}

public sealed record RoleDto(Guid Id, string Name, bool IsSystemRole, string[] Permissions)
{
    public RoleDto(Role role)
        : this(role.Id, role.Name, role.IsSystemRole, role.Permissions.ToArray())
    {
    }
}

public sealed record CreateRoleRequest(
    string Name,
    string[] Permissions,
    bool IsSystemRole = false);

public sealed record UpdateRoleRequest(
    string Name,
    string[] Permissions,
    bool IsSystemRole = false);
