using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Time;
using System.Text.Json;

namespace BotPulse.Infrastructure.Alerts.Rules;

/// <summary>Raises an alert when N jobs fail within a time window.</summary>
public sealed class JobsFailedInWindowEvaluator : IAlertRuleEvaluator
{
    public string RuleType => "JobsFailedInWindow";
    private readonly IJobRepository _jobs;
    private readonly ISystemClock _clock;

    public JobsFailedInWindowEvaluator(IJobRepository jobs, ISystemClock clock)
    {
        _jobs = jobs;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRuleContext rule, CancellationToken ct = default)
    {
        var (threshold, windowMinutes) = GetParams(rule.ParametersJson);
        var since = _clock.UtcNow.AddMinutes(-windowMinutes);

        var (failed, _) = await _jobs.QueryAsync(
            new JobFilter(FromUtc: since, Status: "Failed"),
            ct).ConfigureAwait(false);

        if (failed.Count < threshold)
        {
            return [];
        }

        return [new AlertCandidate(
            "Jobs", "global",
            $"{failed.Count} jobs failed in the last {windowMinutes} minutes (threshold: {threshold})")];
    }

    private static (int threshold, int windowMinutes) GetParams(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var t = doc.RootElement.TryGetProperty("threshold", out var tv) ? tv.GetInt32() : 5;
            var w = doc.RootElement.TryGetProperty("windowMinutes", out var wv) ? wv.GetInt32() : 60;
            return (t, w);
        }
        catch { return (5, 60); }
    }
}
