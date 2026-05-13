using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.DTOs.Reports;
using ReportingPlatform.Application.Interfaces;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/report-executions")]
public sealed class ReportExecutionsController : ControllerBase
{
    private readonly IReportExecutionService _executionService;
    private readonly IReportExecutionRepository _executionRepository;

    public ReportExecutionsController(
        IReportExecutionService executionService,
        IReportExecutionRepository executionRepository)
    {
        _executionService = executionService;
        _executionRepository = executionRepository;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run(
        [FromBody] RunReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var executionId = await _executionService.SubmitAsync(request, cancellationToken);

            return Ok(new
            {
                executionId,
                status = "Pending"
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? templateId, CancellationToken cancellationToken)
    {
        var executions = await _executionRepository.ListAsync(templateId, cancellationToken);

        return Ok(executions);
    }

    [HttpGet("{executionId:guid}")]
    public async Task<IActionResult> Get(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await _executionService.GetExecutionAsync(executionId, cancellationToken);

        return execution is null ? NotFound() : Ok(execution);
    }

    [HttpPost("{executionId:guid}/process")]
    public async Task<IActionResult> Process(Guid executionId, CancellationToken cancellationToken)
    {
        try
        {
            await _executionService.ProcessExecutionAsync(executionId, cancellationToken);
            var execution = await _executionService.GetExecutionAsync(executionId, cancellationToken);

            return execution is null ? NotFound() : Ok(execution);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
