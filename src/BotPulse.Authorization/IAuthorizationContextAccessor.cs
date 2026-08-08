namespace BotPulse.Authorization;

/// <summary>
/// Provides access to the current request's resolved authorization context.
/// Registered as Scoped in DI — one instance per HTTP request.
/// Set by AuthorizationContextMiddleware after authentication.
/// </summary>
public interface IAuthorizationContextAccessor
{
    AuthorizationContext? Current { get; set; }
}
