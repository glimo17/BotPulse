using BotPulse.Core.Application.Queues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class QueuesController : ControllerBase
{
    private readonly QueueQueryService _query;
    private readonly QueueAnalyticsService _analytics;

    public QueuesController(QueueQueryService query, QueueAnalyticsService analytics)
    {
        _query = query;
        _analytics = analytics;
    }

    [HttpGet]
    public async Task<IActionResult> GetQueues([FromQuery] bool forceRefresh = false, CancellationToken ct = default)
    {
        var queues = await _query.GetQueuesAsync(forceRefresh, ct);
        return Ok(queues);
    }

    [HttpGet("{queueName}/analytics")]
    public async Task<IActionResult> GetAnalytics(string queueName, CancellationToken ct = default)
    {
        var analytics = await _analytics.GetQueueAnalyticsAsync(queueName, ct);
        return Ok(analytics);
    }
}
