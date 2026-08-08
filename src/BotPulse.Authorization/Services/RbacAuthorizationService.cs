using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotPulse.Authorization.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace BotPulse.Authorization.Services;

public sealed class RbacAuthorizationService : IAuthorizationService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static string CacheKey(Guid userId) => $"permissions:{userId}";

    public RbacAuthorizationService(
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IMemoryCache cache)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<string>> GetPermissionsAsync(Guid userId)
    {
        if (_cache.TryGetValue(CacheKey(userId), out IEnumerable<string>? cached))
        {
            return cached!;
        }

        var permissions = await _roleRepository.GetPermissionsForUserAsync(userId);

        _cache.Set(CacheKey(userId), permissions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration
        });

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var permissions = await GetPermissionsAsync(userId);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AuthorizationContext> GetAuthorizationContextAsync(Guid userId)
    {
        var permissions = await GetPermissionsAsync(userId);
        var roles = await _userRoleRepository.GetRolesByUserIdAsync(userId);

        return new AuthorizationContext
        {
            UserId = userId,
            Permissions = permissions.ToList(),
            Roles = roles.Select(r => r.Name).ToList()
        };
    }

    public Task InvalidateCacheAsync(Guid userId)
    {
        _cache.Remove(CacheKey(userId));
        return Task.CompletedTask;
    }
}
