using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/query-plan")]
public sealed class QueryPlanController : ControllerBase
{
    private readonly IQueryPlanBuilder _queryPlanBuilder;

    public QueryPlanController(IQueryPlanBuilder queryPlanBuilder)
    {
        _queryPlanBuilder = queryPlanBuilder;
    }

    [HttpPost("build")]
    public async Task<IActionResult> Build(
        [FromBody] ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var queryPlan = await _queryPlanBuilder.BuildAsync(request, cancellationToken);

            return Ok(queryPlan);
        }
        catch (InvalidReportQueryException exception)
        {
            return BadRequest(new
            {
                isValid = false,
                errors = exception.Errors
            });
        }
    }
}
