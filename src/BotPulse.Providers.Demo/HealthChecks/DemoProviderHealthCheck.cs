using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BotPulse.Providers.Demo.HealthChecks;

internal sealed class DemoProviderHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Demo provider active"));
}
