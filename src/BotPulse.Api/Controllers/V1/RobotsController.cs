using BotPulse.Core.Application.Robots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class RobotsController : ControllerBase
{
    private readonly RobotQueryService _robots;

    public RobotsController(RobotQueryService robots) => _robots = robots;

    [HttpGet]
    public async Task<IActionResult> GetRobots([FromQuery] bool forceRefresh = false, CancellationToken ct = default)
    {
        var robots = await _robots.GetRobotsAsync(forceRefresh, ct);
        return Ok(robots);
    }

    [HttpGet("{externalId}")]
    public async Task<IActionResult> GetRobot(string externalId, CancellationToken ct = default)
    {
        var robot = await _robots.GetRobotByIdAsync(externalId, ct);
        if (robot is null)
        {
            return NotFound();
        }

        return Ok(robot);
    }
}
