using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Time;
using System.Text.Json;

namespace BotPulse.Infrastructure.Alerts.Rules;

/// <summary>Raises an alert when a machine has been offline longer than the configured threshold.</summary>
public sealed class MachineOfflineEvaluator : IAlertRuleEvaluator
{
    public string RuleType => "MachineOffline";
    private readonly IMachineProvider _machines;
    private readonly ISystemClock _clock;

    public MachineOfflineEvaluator(IMachineProvider machines, ISystemClock clock)
    {
        _machines = machines;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRuleContext rule, CancellationToken ct = default)
    {
        var thresholdMinutes = GetThreshold(rule.ParametersJson);
        var cutoff = _clock.UtcNow.AddMinutes(-thresholdMinutes);
        var machines = await _machines.GetMachinesAsync(ct).ConfigureAwait(false);

        return machines
            .Where(m => m.Status == "Offline" && m.LastHeartbeatUtc < cutoff)
            .Select(m => new AlertCandidate(
                "Machine", m.ExternalId,
                $"Machine '{m.Name}' has been offline since {m.LastHeartbeatUtc:u} (>{thresholdMinutes}min)"))
            .ToList();
    }

    private static int GetThreshold(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("thresholdMinutes", out var v) ? v.GetInt32() : 60;
        }
        catch { return 60; }
    }
}
