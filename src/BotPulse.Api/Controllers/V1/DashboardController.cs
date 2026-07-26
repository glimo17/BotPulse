using BotPulse.Core.Application.Dashboard;
using BotPulse.Core.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardConfigurationService _dashboardService;

    public DashboardController(DashboardConfigurationService dashboardService) =>
        _dashboardService = dashboardService;

    [HttpGet("layout")]
    public async Task<IActionResult> GetLayout(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized();
        }

        var layout = await _dashboardService.GetLayoutAsync(userId, ct);
        return Ok(layout);
    }

    [HttpPut("layout")]
    public async Task<IActionResult> UpdateLayout([FromBody] UpdateLayoutRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized();
        }

        var layout = await _dashboardService.UpdateLayoutAsync(userId, request.WidgetsJson, ct);
        return Ok(layout);
    }

    [HttpPost("layout/reset")]
    public async Task<IActionResult> ResetLayout(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized();
        }

        var roleStr = User.FindFirst(ClaimTypes.Role)?.Value ?? "Viewer";
        var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.Viewer;

        var layout = await _dashboardService.ResetToDefaultAsync(userId, role, ct);
        return Ok(layout);
    }
}

public sealed record UpdateLayoutRequest(string WidgetsJson);
