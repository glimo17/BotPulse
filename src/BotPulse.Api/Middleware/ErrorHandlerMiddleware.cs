using System.Net;
using System.Text.Json;
using BotPulse.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace BotPulse.Api.Middleware;

/// <summary>
/// Global error handler. Maps domain exceptions to structured JSON error responses.
/// Prevents leaking internal details to clients.
/// </summary>
public sealed class ErrorHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly Action<ILogger, string, Exception> LogUnhandled =
        LoggerMessage.Define<string>(
            LogLevel.Error, new EventId(1, "UnhandledException"),
            "Unhandled exception for {CorrelationId}");

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
        LogUnhandled(_logger, correlationId, ex);

        var (statusCode, errorCode, message) = ex switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", ve.Message),
            EntityNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", ex.Message),
            AuthenticationException => (HttpStatusCode.Unauthorized, "UNAUTHENTICATED", "Authentication failed"),
            AuthorizationException => (HttpStatusCode.Forbidden, "FORBIDDEN", "Access denied"),
            ProviderException => (HttpStatusCode.BadGateway, "PROVIDER_ERROR", "RPA provider error"),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "An unexpected error occurred"),
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = new
        {
            errorCode,
            message,
            correlationId,
            timestamp = DateTime.UtcNow,
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(body, JsonOptions))
            .ConfigureAwait(false);
    }
}
