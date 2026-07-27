using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BotPulse.Authorization.Repositories;

namespace BotPulse.Authorization.Services;

public class RbacAuthorizationService : IAuthorizationService
{
    private readonly IRoleRepository _roleRepository;

    public RbacAuthorizationService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IEnumerable<string>> GetPermissionsAsync(Guid userId)
    {
        // TODO: Resolve user roles via User-Role mapping (integration with identity/user store)
        // For now return an empty list as a placeholder.
        return Array.Empty<string>();
    }
}
