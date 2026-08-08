using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BotPulse.Authorization.Permissions;
using BotPulse.Authorization.Repositories;
using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using BotPulse.Core.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacAuthorizationService = BotPulse.Authorization.IAuthorizationService;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly RbacAuthorizationService _authorizationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UsersController(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        RbacAuthorizationService authorizationService,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _authorizationService = authorizationService;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <summary>List all users with their current roles and last login.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.UsersView)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _userRepository.GetAllAsync(ct);

        var dtos = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id);
            dtos.Add(new UserListDto(user, roles.Select(r => r.Name).ToArray()));
        }

        return Ok(dtos);
    }

    /// <summary>Get a single user by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.UsersView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id);
        return Ok(new UserDetailDto(user, roles.Select(r => r.Name).ToArray()));
    }

    /// <summary>Create a new local user. Only available when using Local authentication.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionCatalog.UsersCreate)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var existingByUsername = await _userRepository.FindByUserNameAsync(request.UserName, ct);
        if (existingByUsername is not null)
        {
            return Conflict(new { error = $"Username '{request.UserName}' is already taken." });
        }

        var existingByEmail = await _userRepository.FindByEmailAsync(request.Email, ct);
        if (existingByEmail is not null)
        {
            return Conflict(new { error = $"Email '{request.Email}' is already registered." });
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(
            externalId: Guid.NewGuid().ToString(),
            userName: request.UserName,
            email: request.Email,
            role: UserRole.Viewer,
            authProvider: "Local",
            passwordHash: passwordHash);

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = user.Id, version = "1" },
            new UserDetailDto(user, Array.Empty<string>()));
    }

    /// <summary>Update a user's profile (username and email).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.UsersUpdate)]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }

        user.UpdateProfile(request.UserName, request.Email);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id);
        return Ok(new UserDetailDto(user, roles.Select(r => r.Name).ToArray()));
    }

    /// <summary>Assign roles to a user. Replaces all current role assignments.</summary>
    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = PermissionCatalog.UsersUpdate)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }

        // Validate all requested role IDs exist
        var requestedRoles = new List<BotPulse.Authorization.Entities.Role>();
        foreach (var roleId in request.RoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role is null)
            {
                return BadRequest(new { error = $"Role '{roleId}' does not exist." });
            }

            requestedRoles.Add(role);
        }

        // Remove existing assignments
        var currentRoles = await _userRoleRepository.GetRolesByUserIdAsync(id);
        foreach (var role in currentRoles)
        {
            await _userRoleRepository.RemoveRoleAsync(id, role.Id);
        }

        // Assign new roles
        var assignedById = GetCurrentUserId();
        foreach (var role in requestedRoles)
        {
            await _userRoleRepository.AssignRoleAsync(id, role.Id, assignedById);
        }

        await _authorizationService.InvalidateCacheAsync(id);

        return Ok(new { userId = id, roles = requestedRoles.Select(r => r.Name).ToArray() });
    }

    /// <summary>Enable a disabled user account.</summary>
    [HttpPut("{id:guid}/enable")]
    [Authorize(Policy = PermissionCatalog.UsersUpdate)]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }

        user.Activate();
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        await _authorizationService.InvalidateCacheAsync(id);

        return Ok(new { userId = id, isActive = true });
    }

    /// <summary>Disable a user account. A user cannot disable themselves.</summary>
    [HttpPut("{id:guid}/disable")]
    [Authorize(Policy = PermissionCatalog.UsersUpdate)]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == id)
        {
            return BadRequest(new { error = "You cannot disable your own account." });
        }

        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }

        user.Deactivate();
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        await _authorizationService.InvalidateCacheAsync(id);

        return Ok(new { userId = id, isActive = false });
    }

    /// <summary>Reset a local user's password.</summary>
    [HttpPut("{id:guid}/reset-password")]
    [Authorize(Policy = PermissionCatalog.UsersUpdate)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }

        if (!string.Equals(user.AuthProvider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Password reset is only available for local users." });
        }

        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword));
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { message = "Password reset successfully." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var internalId = User.FindFirstValue("internal_user_id");
        return Guid.TryParse(internalId, out var id) ? id : null;
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record UserListDto(
    Guid Id,
    string UserName,
    string Email,
    string[] Roles,
    bool IsActive,
    string AuthProvider,
    DateTime? LastLoginUtc)
{
    public UserListDto(User user, string[] roles)
        : this(user.Id, user.UserName, user.Email, roles, user.IsActive, user.AuthProvider, user.LastLoginUtc)
    {
    }
}

public sealed record UserDetailDto(
    Guid Id,
    string UserName,
    string Email,
    string[] Roles,
    bool IsActive,
    string AuthProvider,
    DateTime? LastLoginUtc,
    DateTime CreatedAtUtc)
{
    public UserDetailDto(User user, string[] roles)
        : this(user.Id, user.UserName, user.Email, roles, user.IsActive, user.AuthProvider,
               user.LastLoginUtc, user.CreatedAtUtc)
    {
    }
}

public sealed record CreateUserRequest(string UserName, string Email, string Password);
public sealed record UpdateUserRequest(string UserName, string Email);
public sealed record AssignRolesRequest(Guid[] RoleIds);
public sealed record ResetPasswordRequest(string NewPassword);
