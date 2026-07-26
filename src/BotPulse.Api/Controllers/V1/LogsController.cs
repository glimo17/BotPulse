using BotPulse.Core.Application.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class LogsController : ControllerBase
{
    private readonly LogQueryService _logs;

    public LogsController(LogQueryService logs) => _logs = logs;

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? jobId, [FromQuery] string? provider,
        [FromQuery] string? severity, [FromQuery] string? keyword,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        var filter = new LogFilter(jobId, provider, severity, from, to, keyword, page, pageSize);
        var logs = await _logs.GetLogsAsync(filter, ct);
        return Ok(logs);
    }
}
