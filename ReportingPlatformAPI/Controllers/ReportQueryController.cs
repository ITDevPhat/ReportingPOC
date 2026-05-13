using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/report-query")]
public sealed class ReportQueryController : ControllerBase
{
    private readonly IQueryExecutor _queryExecutor;

    public ReportQueryController(IQueryExecutor queryExecutor)
    {
        _queryExecutor = queryExecutor;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute(
        [FromBody] ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _queryExecutor.ExecuteAsync(request, cancellationToken);

            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }

            return Ok(result);
        }
        catch (InvalidReportQueryException exception)
        {
            return BadRequest(new
            {
                success = false,
                errors = exception.Errors
            });
        }
    }
}
