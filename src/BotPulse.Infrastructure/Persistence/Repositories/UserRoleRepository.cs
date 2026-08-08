using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotPulse.Authorization.Repositories;
using BotPulse.Infrastructure.Persistence.Entities;
using AuthRole = BotPulse.Authorization.Entities.Role;
using PersistUserRole = BotPulse.Infrastructure.Persistence.Entities.UserRole;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

internal sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly BotPulseDbContext _ctx;

    public UserRoleRepository(BotPulseDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyCollection<AuthRole>> GetRolesByUserIdAsync(Guid userId)
    {
        var roles = await _ctx.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ThenInclude(r => r!.Permissions)
            .Select(ur => ur.Role!)
            .ToListAsync();

        return roles.Select(r => new AuthRole
        {
            Id = r.Id,
            Name = r.Name,
            IsSystemRole = r.IsSystem,
            Description = r.Description ?? string.Empty,
            Permissions = r.Permissions.Select(p => p.Permission).ToList()
        }).ToList();
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedByUserId = null, string? scope = null)
    {
        var exists = await _ctx.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (exists)
        {
            return;
        }

        var userRole = new PersistUserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedByUserId = assignedByUserId,
            AssignedAtUtc = DateTimeOffset.UtcNow,
            Scope = scope
        };

        _ctx.UserRoles.Add(userRole);
        await _ctx.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var userRole = await _ctx.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole is null)
        {
            return;
        }

        _ctx.UserRoles.Remove(userRole);
        await _ctx.SaveChangesAsync();
    }

    public Task<bool> UserHasRoleAsync(Guid userId, Guid roleId)
    {
        return _ctx.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
    }

    public Task<bool> RoleHasUsersAsync(Guid roleId)
    {
        return _ctx.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
    }
}
