using System.Security.Claims;
using BotPulse.Authorization;
using BotPulse.Core.Abstractions.Persistence;

namespace BotPulse.Api.Middleware;

/// <summary>
/// Resolves and stores the AuthorizationContext for the authenticated user.
/// Runs after UseAuthentication() so ClaimsPrincipal is already populated.
/// Sets IAuthorizationContextAccessor.Current for use in controllers and services.
/// </summary>
public sealed class AuthorizationContextMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthorizationService authorizationService,
        IAuthorizationContextAccessor contextAccessor,
        IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var externalId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider   = context.User.FindFirstValue("auth_provider");

            if (!string.IsNullOrEmpty(externalId) && !string.IsNullOrEmpty(provider))
            {
                var user = await userRepository.FindByExternalIdAsync(provider, externalId);

                if (user is not null && user.IsActive)
                {
                    var authContext = await authorizationService.GetAuthorizationContextAsync(user.Id);
                    contextAccessor.Current = authContext;
                }
                else if (user is not null && !user.IsActive)
                {
                    // Disabled users are rejected regardless of valid JWT
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "User account is disabled." });
                    return;
                }
            }
        }

        await _next(context);
    }
}
