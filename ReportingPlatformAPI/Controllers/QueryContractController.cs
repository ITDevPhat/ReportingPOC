using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/query-contract")]
public sealed class QueryContractController : ControllerBase
{
    private readonly IReportQueryValidator _validator;

    public QueryContractController(IReportQueryValidator validator)
    {
        _validator = validator;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate(
        [FromBody] ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _validator.ValidateAsync(request, cancellationToken);

            return Ok(new
            {
                isValid = true,
                message = "Query request is valid."
            });
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

    [HttpPost("normalize")]
    public IActionResult Normalize([FromBody] ReportQueryRequest request)
    {
        var normalizedRequest = _validator.Normalize(request);

        return Ok(normalizedRequest);
    }
}
