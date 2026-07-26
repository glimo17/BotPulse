using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Core.Abstractions.Persistence;
using System.Text.Json;

namespace BotPulse.Infrastructure.Alerts.Rules;

/// <summary>Raises an alert when queue pending items exceed a configured threshold.</summary>
public sealed class QueueBacklogEvaluator : IAlertRuleEvaluator
{
    public string RuleType => "QueueBacklog";
    private readonly IQueueItemRepository _queueItems;

    public QueueBacklogEvaluator(IQueueItemRepository queueItems) => _queueItems = queueItems;

    public async Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRuleContext rule, CancellationToken ct = default)
    {
        var threshold = GetThreshold(rule.ParametersJson);
        var pending = await _queueItems.FindAllAsync(
            q => q.Status == "New" || q.Status == "InProgress", ct).ConfigureAwait(false);

        var byQueue = pending
            .GroupBy(q => q.QueueName)
            .Where(g => g.Count() >= threshold)
            .ToList();

        return byQueue.Select(g => new AlertCandidate(
            "Queue", g.Key,
            $"Queue '{g.Key}' has {g.Count()} pending items (threshold: {threshold})"))
            .ToList();
    }

    private static int GetThreshold(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("threshold", out var v) ? v.GetInt32() : 500;
        }
        catch { return 500; }
    }
}
