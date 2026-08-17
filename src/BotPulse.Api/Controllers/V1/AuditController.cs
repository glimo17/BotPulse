using System;
using System.Threading;
using System.Threading.Tasks;
using BotPulse.Authorization.Permissions;
using BotPulse.Core.Abstractions.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditRepository _auditRepository;

    public AuditController(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    /// <summary>
    /// Query the security audit log. Admins only (Audit.View).
    /// Supports filtering by user, action, and time range.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.AuditView)]
    public async Task<IActionResult> Query(
        [FromQuery] string? userId,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int top = 100,
        CancellationToken ct = default)
    {
        var query = new AuditQuery(
            UserId: string.IsNullOrWhiteSpace(userId) ? null : userId,
            Action: string.IsNullOrWhiteSpace(action) ? null : action,
            FromUtc: from,
            ToUtc: to,
            Top: Math.Clamp(top, 1, 500));

        var records = await _auditRepository.QueryAsync(query, ct);
        return Ok(records);
    }
}
