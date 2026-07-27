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

        // Replace permissions
        _ctx.RemoveRange(entity.Permissions);
        entity.Permissions = role.Permissions.Select(p => new RolePermissionEntry { RoleId = entity.Id, Permission = p }).ToList();

        _ctx.Roles.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    private static AuthRole MapToRole(PersistRole e)
    {
        return new AuthRole
        {
            Id = e.Id,
            Name = e.Name,
            IsSystemRole = e.IsSystem,
            Permissions = e.Permissions.Select(p => p.Permission).ToList()
        };
    }
}
