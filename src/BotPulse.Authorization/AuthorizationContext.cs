using System;
using System.Collections.Generic;
using System.Linq;

namespace BotPulse.Authorization;

/// <summary>
/// Resolved authorization context for the current user.
/// Propagated through IAuthorizationContextAccessor to all modules
/// including the Intelligence Platform (Phase 3).
/// </summary>
public sealed class AuthorizationContext
{
    public Guid UserId { get; init; }

    /// <summary>Reserved for future multi-tenant support.</summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>Reserved for future multi-tenant support.</summary>
    public string? TenantId { get; init; }

    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Returns true if the current context includes the specified permission.
    /// Case-insensitive comparison.
    /// </summary>
    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if the current context includes all specified permissions.
    /// </summary>
    public bool HasAllPermissions(params string[] permissions) =>
        permissions.All(HasPermission);

    /// <summary>
    /// Returns true if the current context includes at least one of the specified permissions.
    /// </summary>
    public bool HasAnyPermission(params string[] permissions) =>
        permissions.Any(HasPermission);
}
