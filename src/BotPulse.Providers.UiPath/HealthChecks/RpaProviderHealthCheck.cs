using BotPulse.Providers.UiPath.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BotPulse.Providers.UiPath.HealthChecks;

/// <summary>Health check that verifies the UiPath OAuth2 token is obtainable.</summary>
internal sealed class RpaProviderHealthCheck : IHealthCheck
{
    private readonly UiPathOAuth2TokenManager _tokenManager;

    public RpaProviderHealthCheck(UiPathOAuth2TokenManager tokenManager) =>
        _tokenManager = tokenManager;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tokenManager.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("UiPath provider reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("UiPath provider unreachable", ex);
        }
    }
}
