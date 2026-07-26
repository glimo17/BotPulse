using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Application.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BotPulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertRepository _alertRepo;
    private readonly AlertAcknowledgmentService _ackService;
    private readonly AlertRuleService _ruleService;

    public AlertsController(IAlertRepository alertRepo, AlertAcknowledgmentService ackService, AlertRuleService ruleService)
    {
        _alertRepo = alertRepo;
        _ackService = ackService;
        _ruleService = ruleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
    {
        var alerts = await _alertRepo.FindAllAsync(_ => true, ct);
        var result = alerts
            .OrderByDescending(a => a.RaisedAtUtc)
            .Take(100)
            .Select(a => new
            {
                a.Id,
                a.Severity,
                a.Acknowledged,
                a.RaisedAtUtc,
                a.AcknowledgedAtUtc,
                a.ConditionDescription,
                a.AffectedResourceType,
                a.AffectedResourceId,
            });
        return Ok(result);
    }

    [HttpPost("{alertId}/ack")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<IActionResult> Acknowledge(Guid alertId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        await _ackService.AcknowledgeAsync(alertId, userId, userName, correlationId, ct);
        return Ok();
    }

    [HttpGet("rules")]
    [Authorize(Policy = "ManageAlertRules")]
    public async Task<IActionResult> GetRules(CancellationToken ct)
    {
        var rules = await _ruleService.GetEnabledRulesAsync(ct);
        return Ok(rules);
    }

    [HttpPost("rules")]
    [Authorize(Policy = "ManageAlertRules")]
    public async Task<IActionResult> CreateRule([FromBody] CreateAlertRuleRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        var rule = await _ruleService.CreateAsync(
            request.Name, request.RuleType, request.Severity,
            request.ParametersJson, request.ChannelsJson,
            userId, userName, correlationId, ct);

        return CreatedAtAction(nameof(GetRules), new { }, rule);
    }

    [HttpPost("rules/{ruleId}/enable")]
    [Authorize(Policy = "ManageAlertRules")]
    public async Task<IActionResult> EnableRule(Guid ruleId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        await _ruleService.EnableAsync(ruleId, userId, userName, correlationId, ct);
        return Ok();
    }

    [HttpPost("rules/{ruleId}/disable")]
    [Authorize(Policy = "ManageAlertRules")]
    public async Task<IActionResult> DisableRule(Guid ruleId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        await _ruleService.DisableAsync(ruleId, userId, userName, correlationId, ct);
        return Ok();
    }
}

public sealed record CreateAlertRuleRequest(
    string Name,
    string RuleType,
    string Severity,
    string ParametersJson = "{}",
    string ChannelsJson = "[\"Log\"]");
