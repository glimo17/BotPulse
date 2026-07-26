using System.Collections.Concurrent;
using BotPulse.Core.Abstractions.Alerts;

namespace BotPulse.Infrastructure.Alerts;

/// <summary>
/// Suppresses duplicate alerts within a configurable time window per (ruleId, resourceId) pair.
/// Property invariant: for any sequence of candidates within the window,
/// ShouldEmit returns true at most once per (rule, resource).
/// </summary>
public sealed class AlertDeduplicator : IAlertDeduplicator
{
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, DateTime> _lastEmitted = new();

    public AlertDeduplicator(TimeSpan? window = null) =>
        _window = window ?? TimeSpan.FromMinutes(5);

    public bool ShouldEmit(AlertRuleContext rule, AlertCandidate candidate, DateTime nowUtc)
    {
        var key = $"{rule.RuleId}:{candidate.AffectedResourceType}:{candidate.AffectedResourceId}";

        if (_lastEmitted.TryGetValue(key, out var lastTime) && nowUtc - lastTime < _window)
        {
            return false;
        }

        _lastEmitted[key] = nowUtc;
        return true;
    }
}
