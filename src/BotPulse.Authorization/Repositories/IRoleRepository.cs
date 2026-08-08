using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BotPulse.Authorization.Entities;

namespace BotPulse.Authorization.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByNameAsync(string name);
    Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId);
    Task<IReadOnlyCollection<Role>> GetRolesByUserIdAsync(Guid userId);
    Task CreateAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(Guid id);
}
