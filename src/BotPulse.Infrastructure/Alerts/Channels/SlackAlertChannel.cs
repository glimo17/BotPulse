using System.Net.Http.Json;
using BotPulse.Core.Abstractions.Alerts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Infrastructure.Alerts.Channels;

/// <summary>Slack webhook alert channel.</summary>
public sealed partial class SlackAlertChannel : IAlertChannel
{
    public string Name => "Slack";
    private readonly SlackAlertOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<SlackAlertChannel> _logger;

    public SlackAlertChannel(
        IOptions<SlackAlertOptions> options,
        HttpClient http,
        ILogger<SlackAlertChannel> logger)
    {
        _options = options.Value;
        _http = http;
        _logger = logger;
    }

    public async Task DeliverAsync(AlertDelivery delivery, CancellationToken ct = default)
    {
        var emoji = delivery.Severity switch { "Critical" => "🔴", "Warning" => "🟡", _ => "🔵" };
        var payload = new
        {
            text = $"{emoji} *[{delivery.Severity}]* {delivery.ConditionDescription}\n" +
                   $">Resource: {delivery.AffectedResourceType}/{delivery.AffectedResourceId} | Rule: {delivery.RuleName}",
        };

        var response = await _http.PostAsJsonAsync(_options.WebhookUrl, payload, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogSlackDelivered(_logger, delivery.RuleName);
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) =>
        Task.FromResult(!string.IsNullOrEmpty(_options.WebhookUrl));

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Slack alert delivered for rule '{Rule}'")]
    private static partial void LogSlackDelivered(ILogger logger, string rule);
}

/// <summary>Options for SlackAlertChannel.</summary>
public sealed class SlackAlertOptions
{
    public string WebhookUrl { get; init; } = string.Empty;
}
