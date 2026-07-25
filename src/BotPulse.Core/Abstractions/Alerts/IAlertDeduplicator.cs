namespace BotPulse.Core.Abstractions.Alerts;

/// <summary>
/// Suppresses duplicate alerts within a configurable time window per (rule, resource) pair.
/// </summary>
public interface IAlertDeduplicator
{
    /// <summary>
    /// Returns true if the alert should be emitted; false if a recent duplicate exists within the window.
    /// </summary>
    bool ShouldEmit(AlertRuleContext rule, AlertCandidate candidate, DateTime nowUtc);
}
