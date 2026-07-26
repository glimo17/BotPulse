using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Application.Jobs;
using BotPulse.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class JobsController : ControllerBase
{
    private readonly JobQueryService _query;
    private readonly JobCommandService _command;

    public JobsController(JobQueryService query, JobCommandService command)
    {
        _query = query;
        _command = command;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? robot, [FromQuery] string? process,
        [FromQuery] string? status, [FromQuery] string? errorType,
        [FromQuery] string? provider,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = true,
        CancellationToken ct = default)
    {
        var filter = new JobFilter(from, to, robot, process, null, status, errorType, provider, page, pageSize, sortBy, sortDesc);
        var (items, total) = await _query.GetJobsAsync(filter, ct);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{providerName}/{externalId}")]
    public async Task<IActionResult> GetJob(string providerName, string externalId, CancellationToken ct = default)
    {
        var job = await _query.GetJobByExternalIdAsync(providerName, externalId, ct);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(job);
    }

    [HttpPost("start")]
    [Authorize(Policy = "JobActions")]
    public async Task<IActionResult> StartJob([FromBody] StartJobApiRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var result = await _command.StartAsync(
            new StartJobRequest(request.ProcessExternalId, request.RobotExternalId,
                request.Parameters?.ToDictionary(k => k.Key, v => (object)v.Value), request.Priority),
            User, correlationId, ct);
        return Ok(new { jobExternalId = result.JobExternalId });
    }

    [HttpPost("{externalId}/stop")]
    [Authorize(Policy = "JobActions")]
    public async Task<IActionResult> StopJob(string externalId, [FromQuery] string provider = "UiPath", CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        await _command.StopAsync(externalId, provider, User, correlationId, ct);
        return Ok();
    }

    [HttpPost("{externalId}/cancel")]
    [Authorize(Policy = "JobActions")]
    public async Task<IActionResult> CancelJob(string externalId, [FromQuery] string provider = "UiPath", CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        await _command.CancelAsync(externalId, provider, User, correlationId, ct);
        return Ok();
    }

    [HttpPost("{externalId}/retry")]
    [Authorize(Policy = "JobActions")]
    public async Task<IActionResult> RetryJob(string externalId, [FromQuery] string provider = "UiPath", CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var result = await _command.RetryAsync(externalId, provider, User, correlationId, ct);
        return Ok(new { jobExternalId = result.JobExternalId });
    }
}
