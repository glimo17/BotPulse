using Microsoft.AspNetCore.Authorization;

namespace BotPulse.Api.Authorization;

/// <summary>
/// ASP.NET Core authorization requirement that maps to a single BotPulse permission string.
/// One requirement is registered per permission constant in PermissionCatalog.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
