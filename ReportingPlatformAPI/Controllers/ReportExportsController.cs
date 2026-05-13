using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.DTOs.Exports;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Reports;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/report-exports")]
public sealed class ReportExportsController : ControllerBase
{
    private readonly IReportExecutionService _executionService;
    private readonly IReportExporter _reportExporter;
    private readonly IArtifactStorage _artifactStorage;

    public ReportExportsController(
        IReportExecutionService executionService,
        IReportExporter reportExporter,
        IArtifactStorage artifactStorage)
    {
        _executionService = executionService;
        _reportExporter = reportExporter;
        _artifactStorage = artifactStorage;
    }

    [HttpPost("from-execution/{executionId:guid}")]
    public async Task<IActionResult> FromExecution(
        Guid executionId,
        [FromBody] CreateExportRequest request,
        CancellationToken cancellationToken)
    {
        var execution = await _executionService.GetExecutionAsync(executionId, cancellationToken);
        if (execution is null)
        {
            return NotFound(new { message = $"Report execution '{executionId}' was not found." });
        }

        if (!string.Equals(execution.Status, ReportExecutionStatus.Completed.ToString(), StringComparison.OrdinalIgnoreCase)
            || execution.Result is null)
        {
            return BadRequest(new { message = "Only completed executions with stored results can be exported." });
        }

        try
        {
            var result = await _reportExporter.ExportAsync(
                execution.Result,
                request.Format,
                request.FileName,
                cancellationToken);

            return Ok(result);
        }
        catch (NotSupportedException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("download/{*artifactKey}")]
    public async Task<IActionResult> Download(string artifactKey, CancellationToken cancellationToken)
    {
        try
        {
            artifactKey = Uri.UnescapeDataString(artifactKey);
            var stream = await _artifactStorage.OpenReadAsync(artifactKey, cancellationToken);
            var fileName = Path.GetFileName(artifactKey);
            var contentType = ResolveContentType(fileName);

            return File(stream, contentType, fileName);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Artifact was not found." });
        }
    }

    private static string ResolveContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
