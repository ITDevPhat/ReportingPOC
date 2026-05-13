using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/sql-generator")]
public sealed class SqlGeneratorController : ControllerBase
{
    private readonly IQueryPlanBuilder _queryPlanBuilder;
    private readonly ISqlGenerator _sqlGenerator;

    public SqlGeneratorController(
        IQueryPlanBuilder queryPlanBuilder,
        ISqlGenerator sqlGenerator)
    {
        _queryPlanBuilder = queryPlanBuilder;
        _sqlGenerator = sqlGenerator;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var queryPlan = await _queryPlanBuilder.BuildAsync(request, cancellationToken);
            var generatedSql = await _sqlGenerator.GenerateAsync(queryPlan, cancellationToken);

            return Ok(generatedSql);
        }
        catch (InvalidReportQueryException exception)
        {
            return BadRequest(new
            {
                isValid = false,
                errors = exception.Errors
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                isValid = false,
                errors = new[] { exception.Message }
            });
        }
    }
}
