using BotPulse.Authorization;
using Microsoft.AspNetCore.Http;

namespace BotPulse.Infrastructure.Authorization;

/// <summary>
/// Stores the resolved AuthorizationContext in the current HTTP request's Items dictionary.
/// Scoped per request — set by AuthorizationContextMiddleware.
/// </summary>
internal sealed class HttpContextAuthorizationContextAccessor : IAuthorizationContextAccessor
{
    private const string Key = "BotPulse.AuthorizationContext";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextAuthorizationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuthorizationContext? Current
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            context.Items.TryGetValue(Key, out var value);
            return value as AuthorizationContext;
        }
        set
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is not null)
            {
                context.Items[Key] = value;
            }
        }
    }
}
