using BotPulse.Core.Application.Alerts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Worker.Services;

/// <summary>
/// Periodically runs the Alert Engine and Escalation Engine.
/// Default interval: 60s.
/// </summary>
public sealed class AlertEvaluationService : SynchronizationServiceBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SynchronizationOptions> _optionsMonitor;

    public AlertEvaluationService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SynchronizationOptions> optionsMonitor,
        ILogger<AlertEvaluationService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    public override string Name => "AlertEvaluation";
    public override SynchronizationOptions Options => _optionsMonitor.Get("AlertEvaluation");

    protected override async Task<long> SyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var alertEngine = scope.ServiceProvider.GetRequiredService<AlertEngine>();
        var escalationEngine = scope.ServiceProvider.GetRequiredService<EscalationEngine>();

        await alertEngine.EvaluateAllAsync(ct).ConfigureAwait(false);
        await escalationEngine.EscalatePendingAsync(ct).ConfigureAwait(false);

        return 1;
    }
}
