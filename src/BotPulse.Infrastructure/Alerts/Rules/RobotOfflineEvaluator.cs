using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Time;
using System.Text.Json;

namespace BotPulse.Infrastructure.Alerts.Rules;

/// <summary>Raises an alert when a robot has been offline longer than the configured threshold.</summary>
public sealed class RobotOfflineEvaluator : IAlertRuleEvaluator
{
    public string RuleType => "RobotOffline";
    private readonly IRobotProvider _robots;
    private readonly ISystemClock _clock;

    public RobotOfflineEvaluator(IRobotProvider robots, ISystemClock clock)
    {
        _robots = robots;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRuleContext rule, CancellationToken ct = default)
    {
        var thresholdMinutes = GetThreshold(rule.ParametersJson);
        var cutoff = _clock.UtcNow.AddMinutes(-thresholdMinutes);
        var robots = await _robots.GetRobotsAsync(ct).ConfigureAwait(false);

        return robots
            .Where(r => r.Status == "Offline" && r.LastHeartbeatUtc < cutoff)
            .Select(r => new AlertCandidate(
                "Robot", r.ExternalId,
                $"Robot '{r.Name}' has been offline since {r.LastHeartbeatUtc:u} (>{thresholdMinutes}min)"))
            .ToList();
    }

    private static int GetThreshold(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("thresholdMinutes", out var v) ? v.GetInt32() : 10;
        }
        catch { return 10; }
    }
}
