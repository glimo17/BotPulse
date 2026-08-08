using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotPulse.Authorization;

public interface IAuthorizationService
{
    /// <summary>Returns all permissions for the given user (cached).</summary>
    Task<IEnumerable<string>> GetPermissionsAsync(Guid userId);

    /// <summary>Returns true if the user has the specified permission.</summary>
    Task<bool> HasPermissionAsync(Guid userId, string permission);

    /// <summary>
    /// Returns the full authorization context for the user.
    /// Includes resolved permissions and role names.
    /// </summary>
    Task<AuthorizationContext> GetAuthorizationContextAsync(Guid userId);

    /// <summary>
    /// Invalidates the permission cache for the given user.
    /// Must be called after any role assignment change.
    /// </summary>
    Task InvalidateCacheAsync(Guid userId);
}
