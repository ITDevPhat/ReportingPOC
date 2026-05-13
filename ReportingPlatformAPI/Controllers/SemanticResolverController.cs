using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.DTOs;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/semantic-resolver")]
public sealed class SemanticResolverController : ControllerBase
{
    private readonly ISemanticResolver _semanticResolver;

    public SemanticResolverController(ISemanticResolver semanticResolver)
    {
        _semanticResolver = semanticResolver;
    }

    [HttpGet("fields/{entityKey}/{fieldKey}")]
    public async Task<IActionResult> ResolveField(
        string entityKey,
        string fieldKey,
        CancellationToken cancellationToken)
    {
        return await ExecuteResolverActionAsync(
            () => _semanticResolver.ResolveFieldAsync(BuildSemanticKey(entityKey, fieldKey), cancellationToken));
    }

    [HttpPost("fields/resolve")]
    public async Task<IActionResult> ResolveFields(
        [FromBody] ResolveFieldsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Fields.Count == 0)
        {
            return BadRequest(new { message = "At least one semantic field is required." });
        }

        return await ExecuteResolverActionAsync(
            () => _semanticResolver.ResolveFieldsAsync(request.Fields, cancellationToken));
    }

    [HttpGet("fields/{entityKey}/{fieldKey}/filterable")]
    public async Task<IActionResult> ValidateFilterable(
        string entityKey,
        string fieldKey,
        CancellationToken cancellationToken)
    {
        var semanticKey = BuildSemanticKey(entityKey, fieldKey);

        return await ExecuteResolverActionAsync(async () =>
        {
            await _semanticResolver.ValidateFieldIsFilterableAsync(semanticKey, cancellationToken);

            return new
            {
                semanticKey,
                isFilterable = true
            };
        });
    }

    [HttpGet("fields/{entityKey}/{fieldKey}/groupable")]
    public async Task<IActionResult> ValidateGroupable(
        string entityKey,
        string fieldKey,
        CancellationToken cancellationToken)
    {
        var semanticKey = BuildSemanticKey(entityKey, fieldKey);

        return await ExecuteResolverActionAsync(async () =>
        {
            await _semanticResolver.ValidateFieldIsGroupableAsync(semanticKey, cancellationToken);

            return new
            {
                semanticKey,
                isGroupable = true
            };
        });
    }

    private static string BuildSemanticKey(string entityKey, string fieldKey)
    {
        return $"{entityKey}.{fieldKey}";
    }

    private async Task<IActionResult> ExecuteResolverActionAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();

            return Ok(result);
        }
        catch (InvalidSemanticKeyException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (SemanticEntityNotFoundException exception)
        {
            return NotFound(new { message = exception.Message, entityKey = exception.EntityKey });
        }
        catch (SemanticFieldNotFoundException exception)
        {
            return NotFound(new { message = exception.Message, semanticKey = exception.SemanticKey });
        }
        catch (SemanticFieldCapabilityException exception)
        {
            return BadRequest(new
            {
                message = exception.Message,
                semanticKey = exception.SemanticKey,
                capability = exception.Capability
            });
        }
    }
}
