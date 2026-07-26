using BotPulse.Core.Application.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class MetricsController : ControllerBase
{
    private readonly MetricsQueryService _metrics;

    public MetricsController(MetricsQueryService metrics) => _metrics = metrics;

    [HttpGet("raw")]
    public async Task<IActionResult> GetRaw(
        [FromQuery] string metric, [FromQuery] DateTime from, [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        var points = await _metrics.GetRawMetricsAsync(metric, from, to, ct);
        return Ok(points);
    }

    [HttpGet("rollups")]
    public async Task<IActionResult> GetRollups(
        [FromQuery] string metric, [FromQuery] string granularity = "Hourly",
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var fromUtc = from ?? DateTime.UtcNow.AddDays(-7);
        var toUtc = to ?? DateTime.UtcNow;
        var rollups = await _metrics.GetRollupsAsync(metric, granularity, fromUtc, toUtc, ct);
        return Ok(rollups);
    }
}
