using System.Globalization;
using System.Security.Claims;
using BotPulse.Authorization;
using BotPulse.Authorization.Permissions;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Intelligence.Contracts.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotPulse.Api.Controllers.V1;

/// <summary>
/// Exposes the Intelligence Platform's diagnostic capability (ADR-016).
/// The Core only ever depends on IDiagnosticService — no LLM, embedding, or
/// vector store type is referenced here (ADR-016 §1, §4).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class IntelligenceController : ControllerBase
{
    private readonly IDiagnosticService _diagnosticService;
    private readonly IJobRepository _jobRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IAuthorizationContextAccessor _authorizationContext;

    public IntelligenceController(
        IDiagnosticService diagnosticService,
        IJobRepository jobRepository,
        IAuditRepository auditRepository,
        IAuthorizationContextAccessor authorizationContext)
    {
        _diagnosticService = diagnosticService;
        _jobRepository = jobRepository;
        _auditRepository = auditRepository;
        _authorizationContext = authorizationContext;
    }

    /// <summary>
    /// Produces an AI-assisted diagnosis for a failed job (Requisito 5).
    /// The job must belong to the caller's organization/tenant scope
    /// (ADR-016 §11 tenant isolation) and the caller must hold
    /// Intelligence.Diagnose (RBAC — ADR-017).
    /// </summary>
    [HttpPost("diagnose/{providerName}/{externalJobId}")]
    [Authorize(Policy = PermissionCatalog.IntelligenceDiagnose)]
    public async Task<IActionResult> Diagnose(
        string providerName, string externalJobId, CancellationToken ct = default)
    {
        var job = await _jobRepository.GetByExternalIdAsync(providerName, externalJobId, ct)
            .ConfigureAwait(false);

        if (job is null)
        {
            return NotFound(new { error = $"Job '{externalJobId}' was not found for provider '{providerName}'." });
        }

        if (string.IsNullOrWhiteSpace(job.ErrorMessage))
        {
            return BadRequest(new { error = "This job has no error information to diagnose." });
        }

        var authContext = _authorizationContext.Current;
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        var jobId = job.Id.ToString(CultureInfo.InvariantCulture);

        var request = new DiagnosisRequest(
            JobId: jobId,
            ErrorMessage: job.ErrorMessage,
            StackTrace: null,
            ProcessName: job.ProcessExternalId,
            RobotName: job.RobotExternalId,
            OrganizationId: authContext?.OrganizationId);

        DiagnosisResult result;
        var outcome = "Success";

        try
        {
            result = await _diagnosticService.DiagnoseAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            outcome = "Failure";
            await RecordAuditAsync(jobId, correlationId, outcome).ConfigureAwait(false);
            throw;
        }

        await RecordAuditAsync(jobId, correlationId, outcome).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Records every diagnosis attempt in the security audit log, regardless
    /// of outcome (ADR-016 §11: "All AI operations ... must be auditable").
    /// </summary>
    private async Task RecordAuditAsync(string jobId, string correlationId, string outcome)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _auditRepository.RecordAsync(new AuditRecordData(
            UserId: userId,
            UserName: userName,
            Action: "Intelligence.Diagnose",
            ResourceType: "Job",
            ResourceId: jobId,
            Outcome: outcome,
            IpAddress: ipAddress,
            CorrelationId: correlationId)).ConfigureAwait(false);
    }
}
