using System.Net.Http.Json;
using BotPulse.Core.Abstractions.Alerts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Infrastructure.Alerts.Channels;

/// <summary>Microsoft Teams webhook alert channel.</summary>
public sealed partial class TeamsAlertChannel : IAlertChannel
{
    public string Name => "Teams";
    private readonly TeamsAlertOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<TeamsAlertChannel> _logger;

    public TeamsAlertChannel(
        IOptions<TeamsAlertOptions> options,
        HttpClient http,
        ILogger<TeamsAlertChannel> logger)
    {
        _options = options.Value;
        _http = http;
        _logger = logger;
    }

    public async Task DeliverAsync(AlertDelivery delivery, CancellationToken ct = default)
    {
        var color = delivery.Severity switch { "Critical" => "FF0000", "Warning" => "FFA500", _ => "0078D4" };
        var payload = new
        {
            type = "MessageCard",
            context = "https://schema.org/extensions",
            themeColor = color,
            summary = delivery.ConditionDescription,
            sections = new[]
            {
                new
                {
                    activityTitle = $"[{delivery.Severity}] BotPulse Alert",
                    activitySubtitle = delivery.RuleName,
                    facts = new[]
                    {
                        new { name = "Description", value = delivery.ConditionDescription },
                        new { name = "Resource", value = $"{delivery.AffectedResourceType}/{delivery.AffectedResourceId}" },
                        new { name = "Raised At", value = delivery.RaisedAtUtc.ToString("u") },
                    },
                },
            },
        };

        var response = await _http.PostAsJsonAsync(_options.WebhookUrl, payload, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogTeamsDelivered(_logger, delivery.RuleName);
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) =>
        Task.FromResult(!string.IsNullOrEmpty(_options.WebhookUrl));

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Teams alert delivered for rule '{Rule}'")]
    private static partial void LogTeamsDelivered(ILogger logger, string rule);
}

/// <summary>Options for TeamsAlertChannel.</summary>
public sealed class TeamsAlertOptions
{
    public string WebhookUrl { get; init; } = string.Empty;
}
