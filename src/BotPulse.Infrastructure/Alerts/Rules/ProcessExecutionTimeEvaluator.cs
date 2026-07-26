using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Time;
using System.Text.Json;

namespace BotPulse.Infrastructure.Alerts.Rules;

/// <summary>Raises an alert when a job's duration exceeds the expected threshold for its process.</summary>
public sealed class ProcessExecutionTimeEvaluator : IAlertRuleEvaluator
{
    public string RuleType => "ProcessExecutionTime";
    private readonly IJobRepository _jobs;
    private readonly ISystemClock _clock;

    public ProcessExecutionTimeEvaluator(IJobRepository jobs, ISystemClock clock)
    {
        _jobs = jobs;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRuleContext rule, CancellationToken ct = default)
    {
        var (processId, maxDurationMinutes) = GetParams(rule.ParametersJson);
        var since = _clock.UtcNow.AddHours(-1);

        var filter = new JobFilter(FromUtc: since, Status: "Running");
        if (!string.IsNullOrEmpty(processId))
        {
            filter = filter with { ProcessExternalId = processId };
        }

        var (runningJobs, _) = await _jobs.QueryAsync(filter, ct).ConfigureAwait(false);

        return runningJobs
            .Where(j => j.Duration.HasValue && j.Duration.Value.TotalMinutes > maxDurationMinutes)
            .Select(j => new AlertCandidate(
                "Job", j.ExternalJobId,
                $"Job '{j.ExternalJobId}' (process: {j.ProcessExternalId}) has been running for {j.Duration!.Value.TotalMinutes:F1} min (threshold: {maxDurationMinutes} min)"))
            .ToList();
    }

    private static (string? processId, double maxMinutes) GetParams(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var p = doc.RootElement.TryGetProperty("processExternalId", out var pv) ? pv.GetString() : null;
            var m = doc.RootElement.TryGetProperty("maxDurationMinutes", out var mv) ? mv.GetDouble() : 60.0;
            return (p, m);
        }
        catch { return (null, 60.0); }
    }
}
