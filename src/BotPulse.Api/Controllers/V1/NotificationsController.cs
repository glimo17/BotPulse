using BotPulse.Core.Abstractions.Notifications;
using BotPulse.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationDelivery _delivery;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public NotificationsController(INotificationDelivery delivery) => _delivery = delivery;

    /// <summary>Server-Sent Events stream. Clients connect and receive events as they occur.</summary>
    [HttpGet("stream")]
    public async Task StreamAsync([FromQuery] string[]? events = null, CancellationToken ct = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var subscription = new NotificationSubscription(userId, events ?? [], null);

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await foreach (var evt in _delivery.SubscribeAsync(subscription, ct).ConfigureAwait(false))
        {
            var data = JsonSerializer.Serialize(evt, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes($"event: {evt.EventType}\ndata: {data}\n\n");
            await Response.Body.WriteAsync(bytes, ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>Polling endpoint for clients that cannot use SSE.</summary>
    [HttpGet("pull")]
    public IActionResult PullAsync(
        [FromQuery] DateTime? since = null,
        [FromQuery] string[]? events = null)
    {
        if (_delivery is not PollingNotificationDelivery polling)
        {
            return Ok(new { events = Array.Empty<object>(), message = "Use /stream for SSE transport" });
        }

        var sinceUtc = since ?? DateTime.UtcNow.AddSeconds(-30);
        var result = polling.GetEventsSince(sinceUtc, events);
        return Ok(new { events = result, since = sinceUtc });
    }
}
