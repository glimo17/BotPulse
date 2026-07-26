using BotPulse.Core.Application.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "ViewAssets")]
public sealed class AssetsController : ControllerBase
{
    private readonly AssetQueryService _assets;

    public AssetsController(AssetQueryService assets) => _assets = assets;

    [HttpGet]
    public async Task<IActionResult> GetAssets(CancellationToken ct = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var assets = await _assets.GetAssetsAsync(userId, userName, correlationId, ipAddress, ct);
        return Ok(assets);
    }
}
