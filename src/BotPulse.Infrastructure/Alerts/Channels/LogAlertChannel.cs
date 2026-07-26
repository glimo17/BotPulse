using BotPulse.Core.Abstractions.Alerts;
using Microsoft.Extensions.Logging;

namespace BotPulse.Infrastructure.Alerts.Channels;

/// <summary>Alert channel that writes to Serilog. Always enabled.</summary>
public sealed partial class LogAlertChannel : IAlertChannel
{
    public string Name => "Log";
    private readonly ILogger<LogAlertChannel> _logger;

    public LogAlertChannel(ILogger<LogAlertChannel> logger) => _logger = logger;

    public Task DeliverAsync(AlertDelivery delivery, CancellationToken ct = default)
    {
        switch (delivery.Severity)
        {
            case "Critical":
                LogAlertCritical(_logger, delivery.Severity, delivery.ConditionDescription,
                    delivery.AffectedResourceType, delivery.AffectedResourceId, delivery.AlertId);
                break;
            case "Warning":
                LogAlertWarning(_logger, delivery.Severity, delivery.ConditionDescription,
                    delivery.AffectedResourceType, delivery.AffectedResourceId, delivery.AlertId);
                break;
            default:
                LogAlertInfo(_logger, delivery.Severity, delivery.ConditionDescription,
                    delivery.AffectedResourceType, delivery.AffectedResourceId, delivery.AlertId);
                break;
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);

    [LoggerMessage(EventId = 1, Level = LogLevel.Critical,
        Message = "[ALERT] [{Severity}] {Description} | Resource: {ResourceType}/{ResourceId} | AlertId: {AlertId}")]
    private static partial void LogAlertCritical(ILogger logger,
        string severity, string description, string resourceType, string resourceId, Guid alertId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "[ALERT] [{Severity}] {Description} | Resource: {ResourceType}/{ResourceId} | AlertId: {AlertId}")]
    private static partial void LogAlertWarning(ILogger logger,
        string severity, string description, string resourceType, string resourceId, Guid alertId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information,
        Message = "[ALERT] [{Severity}] {Description} | Resource: {ResourceType}/{ResourceId} | AlertId: {AlertId}")]
    private static partial void LogAlertInfo(ILogger logger,
        string severity, string description, string resourceType, string resourceId, Guid alertId);
}
