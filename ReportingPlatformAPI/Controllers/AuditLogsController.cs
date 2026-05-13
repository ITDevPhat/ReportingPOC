using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.Interfaces;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogsController(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventType,
        CancellationToken cancellationToken)
    {
        var logs = await _auditLogRepository.ListAsync(from, to, eventType, cancellationToken);

        return Ok(logs);
    }
}
