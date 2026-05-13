using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.DTOs;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/relationship-resolver")]
public sealed class RelationshipResolverController : ControllerBase
{
    private readonly IRelationshipResolver _relationshipResolver;

    public RelationshipResolverController(IRelationshipResolver relationshipResolver)
    {
        _relationshipResolver = relationshipResolver;
    }

    [HttpGet("path/{baseEntityKey}/{targetEntityKey}")]
    public async Task<IActionResult> ResolvePath(
        string baseEntityKey,
        string targetEntityKey,
        CancellationToken cancellationToken)
    {
        return await ExecuteResolverActionAsync(
            () => _relationshipResolver.ResolvePathAsync(baseEntityKey, targetEntityKey, cancellationToken));
    }

    [HttpPost("paths")]
    public async Task<IActionResult> ResolvePaths(
        [FromBody] ResolveRelationshipPathsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TargetEntityKeys.Count == 0)
        {
            return BadRequest(new { message = "At least one target entity key is required." });
        }

        return await ExecuteResolverActionAsync(
            () => _relationshipResolver.ResolvePathsAsync(request.BaseEntityKey, request.TargetEntityKeys, cancellationToken));
    }

    private async Task<IActionResult> ExecuteResolverActionAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (AmbiguousRelationshipPathException exception)
        {
            return BadRequest(new
            {
                message = exception.Message,
                baseEntityKey = exception.BaseEntityKey,
                targetEntityKey = exception.TargetEntityKey
            });
        }
        catch (RelationshipPathNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message,
                baseEntityKey = exception.BaseEntityKey,
                targetEntityKey = exception.TargetEntityKey
            });
        }
    }
}
