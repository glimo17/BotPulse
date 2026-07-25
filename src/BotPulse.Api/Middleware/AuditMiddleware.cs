using System.Security.Claims;
using BotPulse.Core.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace BotPulse.Api.Middleware;

/// <summary>
/// Records audit entries for sensitive actions.
/// Triggered by specific HTTP methods on protected routes.
/// </summary>
public sealed class AuditMiddleware
{
    private static readonly HashSet<string> AuditedMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly Action<ILogger, string, Exception> LogAuditFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error, new EventId(1, "AuditFailed"),
            "Failed to write audit record for {CorrelationId}");

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditRepository auditRepository)
    {
        await _next(context).ConfigureAwait(false);

        if (!ShouldAudit(context))
        {
            return;
        }

        var user = context.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "anonymous";
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var outcome = context.Response.StatusCode < 400 ? "Success" : "Error";

        try
        {
            await auditRepository.RecordAsync(new AuditRecordData(
                UserId: userId,
                UserName: userName,
                Action: $"{context.Request.Method}:{context.Request.Path}",
                ResourceType: ExtractResourceType(context.Request.Path),
                ResourceId: null,
                Outcome: outcome,
                IpAddress: ipAddress,
                CorrelationId: correlationId)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogAuditFailed(_logger, correlationId, ex);
        }
    }

    private static bool ShouldAudit(HttpContext context) =>
        AuditedMethods.Contains(context.Request.Method) &&
        context.User.Identity?.IsAuthenticated == true &&
        context.Request.Path.StartsWithSegments("/api");

    private static string ExtractResourceType(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments?.Length >= 2 ? segments[2] : "Unknown";
    }
}
