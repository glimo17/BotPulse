using System.Net.Http.Json;
using BotPulse.Core.Abstractions.Alerts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Infrastructure.Alerts.Channels;

/// <summary>Generic HTTP webhook alert channel.</summary>
public sealed partial class WebhookAlertChannel : IAlertChannel
{
    public string Name => "Webhook";
    private readonly WebhookAlertOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<WebhookAlertChannel> _logger;

    public WebhookAlertChannel(
        IOptions<WebhookAlertOptions> options,
        HttpClient http,
        ILogger<WebhookAlertChannel> logger)
    {
        _options = options.Value;
        _http = http;
        _logger = logger;
    }

    public async Task DeliverAsync(AlertDelivery delivery, CancellationToken ct = default)
    {
        var payload = new
        {
            alertId = delivery.AlertId,
            ruleId = delivery.RuleId,
            ruleName = delivery.RuleName,
            severity = delivery.Severity,
            description = delivery.ConditionDescription,
            resourceType = delivery.AffectedResourceType,
            resourceId = delivery.AffectedResourceId,
            raisedAt = delivery.RaisedAtUtc,
            escalationLevel = delivery.EscalationLevel,
        };

        var response = await _http.PostAsJsonAsync(_options.Url, payload, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogWebhookDelivered(_logger, _options.Url);
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) =>
        Task.FromResult(!string.IsNullOrEmpty(_options.Url));

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Webhook alert delivered to {Url}")]
    private static partial void LogWebhookDelivered(ILogger logger, string url);
}

/// <summary>Options for WebhookAlertChannel.</summary>
public sealed class WebhookAlertOptions
{
    public string Url { get; init; } = string.Empty;
}
