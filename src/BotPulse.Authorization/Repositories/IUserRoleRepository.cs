using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BotPulse.Authorization.Entities;

namespace BotPulse.Authorization.Repositories;

public interface IUserRoleRepository
{
    /// <summary>Returns all roles assigned to a user.</summary>
    Task<IReadOnlyCollection<Role>> GetRolesByUserIdAsync(Guid userId);

    /// <summary>
    /// Assigns a role to a user.
    /// If the assignment already exists, the operation is a no-op.
    /// </summary>
    Task AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedByUserId = null, string? scope = null);

    /// <summary>Removes a role assignment from a user.</summary>
    Task RemoveRoleAsync(Guid userId, Guid roleId);

    /// <summary>Returns true if the user has the specified role assigned.</summary>
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId);

    /// <summary>
    /// Returns true if any user is currently assigned to this role.
    /// Used to guard against deleting roles that are in use.
    /// </summary>
    Task<bool> RoleHasUsersAsync(Guid roleId);
}
