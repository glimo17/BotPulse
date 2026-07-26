using BotPulse.Core.Application.Processes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class ProcessesController : ControllerBase
{
    private readonly ProcessQueryService _processes;

    public ProcessesController(ProcessQueryService processes) => _processes = processes;

    [HttpGet]
    public async Task<IActionResult> GetProcesses([FromQuery] bool forceRefresh = false, CancellationToken ct = default)
    {
        var processes = await _processes.GetProcessesAsync(forceRefresh, ct);
        return Ok(processes);
    }

    [HttpGet("{externalId}/parameters")]
    public async Task<IActionResult> GetParameters(string externalId, CancellationToken ct = default)
    {
        var parameters = await _processes.GetProcessParametersAsync(externalId, ct);
        return Ok(parameters);
    }
}
