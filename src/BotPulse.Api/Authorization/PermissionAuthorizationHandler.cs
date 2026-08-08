using BotPulse.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BotPulse.Api.Authorization;

/// <summary>
/// Evaluates PermissionRequirement against the current user's AuthorizationContext.
/// Reads context from IAuthorizationContextAccessor (set by AuthorizationContextMiddleware).
/// Falls back to permission claims on ClaimsPrincipal if context is not yet available.
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IAuthorizationContextAccessor _contextAccessor;

    public PermissionAuthorizationHandler(IAuthorizationContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Primary: use resolved AuthorizationContext (set by middleware)
        var authContext = _contextAccessor.Current;
        if (authContext is not null)
        {
            if (authContext.HasPermission(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        // Fallback: read permission claims added by OnTokenValidated
        var hasClaim = context.User.Claims
            .Any(c => c.Type == "permission" &&
                      string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasClaim)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
