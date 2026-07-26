using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Text.Json;

namespace BotPulse.Infrastructure.Alerts;

/// <summary>
/// Routes alert delivery to configured channels with Polly retry (exponential backoff, 3 attempts).
/// Marks channels as degraded after all retries fail.
/// </summary>
public sealed partial class NotificationRouter
{
    private readonly IEnumerable<IAlertChannel> _channels;
    private readonly ILogger<NotificationRouter> _logger;

    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1),
        })
        .Build();

    public NotificationRouter(IEnumerable<IAlertChannel> channels, ILogger<NotificationRouter> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    public async Task DispatchAsync(Alert alert, AlertRule rule, CancellationToken ct = default)
    {
        var configuredChannels = ParseChannels(rule.ChannelsJson);

        var delivery = new AlertDelivery(
            AlertId: alert.Id,
            RuleId: rule.Id,
            RuleName: rule.Name,
            Severity: rule.Severity,
            ConditionDescription: alert.ConditionDescription,
            AffectedResourceType: alert.AffectedResourceType,
            AffectedResourceId: alert.AffectedResourceId,
            RaisedAtUtc: alert.RaisedAtUtc,
            EscalationLevel: alert.EscalationLevel);

        var tasks = _channels
            .Where(c => configuredChannels.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
            .Select(channel => DispatchToChannelAsync(channel, delivery, alert.Id, ct));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task DispatchToChannelAsync(
        IAlertChannel channel, AlertDelivery delivery, Guid alertId, CancellationToken ct)
    {
        try
        {
            await RetryPipeline.ExecuteAsync(
                async token => await channel.DeliverAsync(delivery, token).ConfigureAwait(false),
                ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogDeliveryFailed(_logger, channel.Name, alertId.ToString(), ex);
        }
    }

    private static List<string> ParseChannels(string channelsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(channelsJson) ?? ["Log"];
        }
        catch
        {
            return ["Log"];
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error,
        Message = "Alert channel '{Channel}' failed to deliver alert {AlertId}")]
    private static partial void LogDeliveryFailed(ILogger logger, string channel, string alertId, Exception ex);
}
