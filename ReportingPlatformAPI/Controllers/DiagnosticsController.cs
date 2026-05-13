using Dapper;
using Microsoft.AspNetCore.Mvc;
using ReportingPlatform.Infrastructure.Persistence;

namespace ReportingPlatform.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DiagnosticsController(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        using var connection = _connectionFactory.CreateConnection();

        var utcTime = await connection.QuerySingleAsync<DateTime>(
            "SELECT SYSUTCDATETIME();");

        return Ok(new
        {
            status = "DB_OK",
            utcTime
        });
    }
}