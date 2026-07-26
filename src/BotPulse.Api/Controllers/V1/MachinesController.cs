using BotPulse.Core.Application.Machines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class MachinesController : ControllerBase
{
    private readonly MachineQueryService _machines;

    public MachinesController(MachineQueryService machines) => _machines = machines;

    [HttpGet]
    public async Task<IActionResult> GetMachines([FromQuery] bool forceRefresh = false, CancellationToken ct = default)
    {
        var machines = await _machines.GetMachinesAsync(forceRefresh, ct);
        return Ok(machines);
    }

    [HttpGet("{externalId}")]
    public async Task<IActionResult> GetMachine(string externalId, CancellationToken ct = default)
    {
        var machine = await _machines.GetMachineByIdAsync(externalId, ct);
        if (machine is null)
        {
            return NotFound();
        }

        return Ok(machine);
    }
}
