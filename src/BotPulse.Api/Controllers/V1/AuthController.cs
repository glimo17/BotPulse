using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Core.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthenticationOrchestrator _auth;

    public AuthController(AuthenticationOrchestrator auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _auth.LoginAsync(
            new AuthenticationRequest(request.UserName, request.Password, null),
            correlationId, ipAddress, ct);

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = result.FailureReason });
        }

        return Ok(new { token = result.Token });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        await _auth.LogoutAsync(userId, userName, correlationId, ct: ct);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new { userId, userName, email, roles });
    }
}

public sealed record LoginRequest(string UserName, string Password);
