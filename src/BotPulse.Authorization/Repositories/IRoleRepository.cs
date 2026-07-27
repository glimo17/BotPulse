using System;
using System.Threading.Tasks;
using BotPulse.Authorization.Entities;

namespace BotPulse.Authorization.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByNameAsync(string name);
        Task CreateAsync(Role role);
        Task UpdateAsync(Role role);
    }
}
