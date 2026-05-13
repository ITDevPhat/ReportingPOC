using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.DTOs.Reports;
using ReportingPlatform.Application.Interfaces;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/report-templates")]
public sealed class ReportTemplatesController : ControllerBase
{
    private readonly IReportTemplateRepository _templateRepository;

    public ReportTemplatesController(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var templateId = await _templateRepository.CreateAsync(request, null, cancellationToken);

            return Ok(new { templateId });
        }
        catch (Exception exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var templates = await _templateRepository.ListAsync(cancellationToken);

        return Ok(templates);
    }

    [HttpGet("{templateId:guid}")]
    public async Task<IActionResult> GetById(Guid templateId, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);

        return template is null ? NotFound() : Ok(template);
    }

    [HttpGet("key/{templateKey}")]
    public async Task<IActionResult> GetByKey(string templateKey, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByKeyAsync(templateKey, cancellationToken);

        return template is null ? NotFound() : Ok(template);
    }

    [HttpPut("{templateId:guid}")]
    public async Task<IActionResult> Update(
        Guid templateId,
        [FromBody] UpdateReportTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await _templateRepository.UpdateAsync(templateId, request, null, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{templateId:guid}")]
    public async Task<IActionResult> Delete(Guid templateId, CancellationToken cancellationToken)
    {
        await _templateRepository.DeactivateAsync(templateId, null, cancellationToken);

        return NoContent();
    }
}
