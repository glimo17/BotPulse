using System;
using System.Linq;
using System.Threading.Tasks;
using BotPulse.Authorization.Repositories;
using BotPulse.Infrastructure.Persistence.Entities;
using AuthRole = BotPulse.Authorization.Entities.Role;
using PersistRole = BotPulse.Infrastructure.Persistence.Entities.Role;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly BotPulseDbContext _ctx;

    public RoleRepository(BotPulseDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyCollection<AuthRole>> GetAllAsync()
    {
        var entities = await _ctx.Roles.Include(r => r.Permissions).ToListAsync();
        return entities.Select(MapToRole).ToList();
    }

    public async Task<AuthRole?> GetByIdAsync(Guid id)
    {
        var entity = await _ctx.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null)
        {
            return null;
        }

        return MapToRole(entity);
    }

    public async Task<AuthRole?> GetByNameAsync(string name)
    {
        var entity = await _ctx.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == name);
        if (entity == null)
        {
            return null;
        }

        return MapToRole(entity);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId)
    {
        return await _ctx.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .SelectMany(ur => ur.Role!.Permissions.Select(p => p.Permission))
            .Distinct()
            .ToListAsync();
    }

    public async Task CreateAsync(AuthRole role)
    {
        var entity = new PersistRole
        {
            Id = role.Id,
            Name = role.Name,
            IsSystem = role.IsSystemRole,
        };

        entity.Permissions.AddRange(role.Permissions.Select(p => new RolePermissionEntry { Permission = p, RoleId = entity.Id }));

        _ctx.Roles.Add(entity);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(AuthRole role)
    {
        var entity = await _ctx.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == role.Id);
        if (entity == null)
        {
            throw new InvalidOperationException("Role not found");
        }

        entity.Name = role.Name;
        entity.IsSystem = role.IsSystemRole;

        _ctx.RemoveRange(entity.Permissions);
        entity.Permissions = role.Permissions.Select(p => new RolePermissionEntry { RoleId = entity.Id, Permission = p }).ToList();

        _ctx.Roles.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.Roles.FindAsync(id);
        if (entity == null)
        {
            return;
        }

        _ctx.Roles.Remove(entity);
        await _ctx.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<AuthRole>> GetRolesByUserIdAsync(Guid userId)
    {
        var roles = await _ctx.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ThenInclude(r => r!.Permissions)
            .Select(ur => ur.Role!)
            .ToListAsync();

        return roles.Select(MapToRole).ToList();
    }

    private static AuthRole MapToRole(PersistRole e)
    {
        return new AuthRole
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description ?? string.Empty,
            IsSystemRole = e.IsSystem,
            Permissions = e.Permissions.Select(p => p.Permission).ToList(),
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
    }
}
