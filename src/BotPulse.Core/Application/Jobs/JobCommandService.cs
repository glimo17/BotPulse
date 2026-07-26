using System.Security.Claims;
using BotPulse.Core.Abstractions.Notifications;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Exceptions;

namespace BotPulse.Core.Application.Jobs;

/// <summary>
/// Command service for job lifecycle actions: start, stop, cancel, retry.
/// All actions are audit-logged and emit notification events.
/// </summary>
public sealed class JobCommandService
{
    private readonly IJobProvider _jobProvider;
    private readonly IJobRepository _jobRepository;
    private readonly IAuditRepository _audit;
    private readonly INotificationDelivery _notifications;

    public JobCommandService(
        IJobProvider jobProvider,
        IJobRepository jobRepository,
        IAuditRepository audit,
        INotificationDelivery notifications)
    {
        _jobProvider = jobProvider;
        _jobRepository = jobRepository;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<StartJobResult> StartAsync(
        StartJobRequest request,
        ClaimsPrincipal user,
        string correlationId,
        CancellationToken ct = default)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

        try
        {
            var result = await _jobProvider.StartJobAsync(request, ct).ConfigureAwait(false);

            await _audit.RecordAsync(new AuditRecordData(
                UserId: userId, UserName: userName, Action: "StartJob",
                ResourceType: "Job", ResourceId: result.JobExternalId,
                Outcome: "Success", IpAddress: null, CorrelationId: correlationId), ct)
                .ConfigureAwait(false);

            await _notifications.PublishAsync(new NotificationEvent(
                "job.action.requested", "Job", result.JobExternalId,
                $"{{\"action\":\"Start\",\"user\":\"{userName}\"}}",
                DateTime.UtcNow), ct).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex) when (ex is not ProviderException && ex is not AuthorizationException)
        {
            await _audit.RecordAsync(new AuditRecordData(
                UserId: userId, UserName: userName, Action: "StartJob",
                ResourceType: "Job", ResourceId: request.ProcessExternalId,
                Outcome: "Error", IpAddress: null, CorrelationId: correlationId,
                DetailsJson: $"{{\"error\":\"{ex.Message}\"}}"), ct)
                .ConfigureAwait(false);
            throw;
        }
    }

    public async Task StopAsync(string jobExternalId, string providerName, ClaimsPrincipal user, string correlationId, CancellationToken ct = default)
    {
        var job = await _jobRepository.GetByExternalIdAsync(providerName, jobExternalId, ct).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("Job", jobExternalId);

        if (!job.Status.IsActive)
        {
            throw new BotPulse.Core.Exceptions.ValidationException(
                [new ValidationError("status", $"Cannot stop a job in '{job.Status}' state.")]);
        }

        await _jobProvider.StopJobAsync(jobExternalId, ct).ConfigureAwait(false);

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId, UserName: userName, Action: "StopJob",
            ResourceType: "Job", ResourceId: jobExternalId,
            Outcome: "Success", IpAddress: null, CorrelationId: correlationId), ct)
            .ConfigureAwait(false);
    }

    public async Task CancelAsync(string jobExternalId, string providerName, ClaimsPrincipal user, string correlationId, CancellationToken ct = default)
    {
        await _jobProvider.CancelJobAsync(jobExternalId, ct).ConfigureAwait(false);

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId, UserName: userName, Action: "CancelJob",
            ResourceType: "Job", ResourceId: jobExternalId,
            Outcome: "Success", IpAddress: null, CorrelationId: correlationId), ct)
            .ConfigureAwait(false);
    }

    public async Task<StartJobResult> RetryAsync(string jobExternalId, string providerName, ClaimsPrincipal user, string correlationId, CancellationToken ct = default)
    {
        var original = await _jobRepository.GetByExternalIdAsync(providerName, jobExternalId, ct).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("Job", jobExternalId);

        if (!original.Status.IsTerminal || original.Status == Core.Domain.ValueObjects.JobStatus.Success)
        {
            throw new BotPulse.Core.Exceptions.ValidationException(
                [new ValidationError("status", $"Cannot retry a job in '{original.Status}' state. Only failed/stopped jobs can be retried.")]);
        }

        var request = new StartJobRequest(original.ProcessExternalId, original.RobotExternalId);
        var result = await _jobProvider.StartJobAsync(request, ct).ConfigureAwait(false);

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId, UserName: userName, Action: "RetryJob",
            ResourceType: "Job", ResourceId: result.JobExternalId,
            Outcome: "Success", IpAddress: null, CorrelationId: correlationId,
            DetailsJson: $"{{\"originalJobId\":\"{jobExternalId}\"}}"), ct)
            .ConfigureAwait(false);

        return result;
    }
}
