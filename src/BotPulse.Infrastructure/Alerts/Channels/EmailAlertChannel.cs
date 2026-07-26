using BotPulse.Core.Abstractions.Alerts;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BotPulse.Infrastructure.Alerts.Channels;

/// <summary>Email alert channel using MailKit SMTP.</summary>
public sealed partial class EmailAlertChannel : IAlertChannel
{
    public string Name => "Email";
    private readonly EmailAlertOptions _options;
    private readonly ILogger<EmailAlertChannel> _logger;

    public EmailAlertChannel(IOptions<EmailAlertOptions> options, ILogger<EmailAlertChannel> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task DeliverAsync(AlertDelivery delivery, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.FromAddress));
        foreach (var to in _options.ToAddresses)
        {
            message.To.Add(MailboxAddress.Parse(to));
        }

        message.Subject = $"[BotPulse Alert] [{delivery.Severity}] {delivery.ConditionDescription}";
        message.Body = new TextPart("plain")
        {
            Text = $"Alert ID: {delivery.AlertId}\n" +
                   $"Severity: {delivery.Severity}\n" +
                   $"Rule: {delivery.RuleName}\n" +
                   $"Description: {delivery.ConditionDescription}\n" +
                   $"Resource: {delivery.AffectedResourceType}/{delivery.AffectedResourceId}\n" +
                   $"Raised at: {delivery.RaisedAtUtc:u}",
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _options.UseSsl, ct)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(_options.SmtpUser) && _options.SmtpPassword is not null)
        {
            await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword, ct)
                .ConfigureAwait(false);
        }

        await client.SendAsync(message, ct).ConfigureAwait(false);
        await client.DisconnectAsync(true, ct).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            var recipients = string.Join(", ", _options.ToAddresses);
            LogEmailSent(_logger, recipients);
        }
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Email alert sent to {Recipients}")]
    private static partial void LogEmailSent(ILogger logger, string recipients);
}

/// <summary>Options for EmailAlertChannel.</summary>
public sealed class EmailAlertOptions
{
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string? SmtpUser { get; init; }
    public string? SmtpPassword { get; init; }
    public string FromAddress { get; init; } = "alerts@botpulse.local";
    public IReadOnlyList<string> ToAddresses { get; init; } = [];
}
