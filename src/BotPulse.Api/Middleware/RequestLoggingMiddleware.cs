using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BotPulse.Api.Middleware;

/// <summary>Logs method, path, status code and duration for every HTTP request.</summary>
public sealed class RequestLoggingMiddleware
{
    private static readonly Action<ILogger, string, string, Exception?> LogRequest =
        LoggerMessage.Define<string, string>(
            LogLevel.Information, new EventId(1, "HttpRequest"),
            "HTTP {Method} {Path}");

    private static readonly Action<ILogger, string, string, int, long, Exception?> LogResponse =
        LoggerMessage.Define<string, string, int, long>(
            LogLevel.Information, new EventId(2, "HttpResponse"),
            "HTTP {Method} {Path} → {StatusCode} ({ElapsedMs}ms)");

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        LogRequest(_logger, context.Request.Method, context.Request.Path, null);

        await _next(context).ConfigureAwait(false);

        sw.Stop();
        LogResponse(_logger, context.Request.Method, context.Request.Path,
            context.Response.StatusCode, sw.ElapsedMilliseconds, null);
    }
}
