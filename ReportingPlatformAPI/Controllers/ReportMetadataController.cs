using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.Interfaces;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/report-metadata")]
public sealed class ReportMetadataController : ControllerBase
{
    private readonly ISemanticMetadataProvider _metadataProvider;

    public ReportMetadataController(ISemanticMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    [HttpGet("entities")]
    public async Task<IActionResult> GetEntities(CancellationToken cancellationToken)
    {
        var entities = await _metadataProvider.GetEntitiesAsync(cancellationToken);

        return Ok(entities);
    }

    [HttpGet("entities/{entityKey}")]
    public async Task<IActionResult> GetEntity(string entityKey, CancellationToken cancellationToken)
    {
        var entity = await _metadataProvider.GetEntityAsync(entityKey, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        return Ok(entity);
    }

    [HttpGet("entities/{entityKey}/fields")]
    public async Task<IActionResult> GetFields(string entityKey, CancellationToken cancellationToken)
    {
        var entity = await _metadataProvider.GetEntityAsync(entityKey, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        var fields = await _metadataProvider.GetFieldsByEntityKeyAsync(entityKey, cancellationToken);

        return Ok(fields);
    }

    [HttpGet("relationships")]
    public async Task<IActionResult> GetRelationships(CancellationToken cancellationToken)
    {
        var relationships = await _metadataProvider.GetRelationshipsAsync(cancellationToken);

        return Ok(relationships);
    }
}
