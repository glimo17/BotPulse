using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "RequireAdministrator")]
public sealed class AdminController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth() => Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
}
