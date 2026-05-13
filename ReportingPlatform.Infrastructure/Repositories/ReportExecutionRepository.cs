using System.Text.Json;
using Dapper;
using ReportingPlatform.Application.DTOs.Reports;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.Reports;
using ReportingPlatform.Domain.Results;
using ReportingPlatform.Infrastructure.Persistence;

namespace ReportingPlatform.Infrastructure.Repositories;

public sealed class ReportExecutionRepository : IReportExecutionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ReportExecutionRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreatePendingAsync(RunReportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Query is null)
        {
            throw new ArgumentException("Execution query is required.");
        }

        const string sql = """
            INSERT INTO rpt.ReportExecutions
            (
                ExecutionId,
                TemplateId,
                Status,
                QueryJson,
                CreatedBy
            )
            VALUES
            (
                @ExecutionId,
                @TemplateId,
                @Status,
                @QueryJson,
                @CreatedBy
            );
            """;

        var executionId = Guid.NewGuid();
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    ExecutionId = executionId,
                    request.TemplateId,
                    Status = ReportExecutionStatus.Pending.ToString(),
                    QueryJson = JsonSerializer.Serialize(request.Query, JsonSerialization.Options),
                    request.CreatedBy
                },
                cancellationToken: cancellationToken));

        return executionId;
    }

    public Task MarkProcessingAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE rpt.ReportExecutions
            SET
                Status = @Status,
                StartedAt = COALESCE(StartedAt, SYSUTCDATETIME())
            WHERE ExecutionId = @ExecutionId;
            """;

        return ExecuteAsync(sql, new { ExecutionId = executionId, Status = ReportExecutionStatus.Processing.ToString() }, cancellationToken);
    }

    public Task MarkCompletedAsync(Guid executionId, QueryExecutionResult result, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE rpt.ReportExecutions
            SET
                Status = @Status,
                GeneratedSql = @GeneratedSql,
                ParametersJson = @ParametersJson,
                ResultJson = @ResultJson,
                [RowCount] = @RowCount,
                ErrorMessage = NULL,
                CompletedAt = SYSUTCDATETIME()
            WHERE ExecutionId = @ExecutionId;
            """;

        return ExecuteAsync(
            sql,
            new
            {
                ExecutionId = executionId,
                Status = ReportExecutionStatus.Completed.ToString(),
                result.GeneratedSql,
                ParametersJson = JsonSerializer.Serialize(result.Parameters, JsonSerialization.Options),
                ResultJson = JsonSerializer.Serialize(result, JsonSerialization.Options),
                result.RowCount
            },
            cancellationToken);
    }

    public Task MarkFailedAsync(Guid executionId, string errorMessage, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE rpt.ReportExecutions
            SET
                Status = @Status,
                ErrorMessage = @ErrorMessage,
                CompletedAt = SYSUTCDATETIME()
            WHERE ExecutionId = @ExecutionId;
            """;

        return ExecuteAsync(
            sql,
            new
            {
                ExecutionId = executionId,
                Status = ReportExecutionStatus.Failed.ToString(),
                ErrorMessage = errorMessage
            },
            cancellationToken);
    }

    public async Task<ReportExecutionDto?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM rpt.ReportExecutions
            WHERE ExecutionId = @ExecutionId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var execution = await connection.QuerySingleOrDefaultAsync<ReportExecution>(
            new CommandDefinition(sql, new { ExecutionId = executionId }, cancellationToken: cancellationToken));

        return execution is null ? null : Map(execution);
    }

    public async Task<IReadOnlyList<ReportExecutionDto>> ListAsync(Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM rpt.ReportExecutions
            WHERE @TemplateId IS NULL OR TemplateId = @TemplateId
            ORDER BY CreatedAt DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var executions = await connection.QueryAsync<ReportExecution>(
            new CommandDefinition(sql, new { TemplateId = templateId }, cancellationToken: cancellationToken));

        return executions.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetPendingExecutionIdsAsync(int take = 10, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Take) ExecutionId
            FROM rpt.ReportExecutions
            WHERE Status = @Status
            ORDER BY CreatedAt;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>(
            new CommandDefinition(
                sql,
                new { Take = take, Status = ReportExecutionStatus.Pending.ToString() },
                cancellationToken: cancellationToken));

        return ids.AsList();
    }

    private async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private static ReportExecutionDto Map(ReportExecution execution)
    {
        return new ReportExecutionDto
        {
            ExecutionId = execution.ExecutionId,
            TemplateId = execution.TemplateId,
            Status = execution.Status,
            Query = JsonSerializer.Deserialize<ReportQueryRequest>(execution.QueryJson, JsonSerialization.Options) ?? new ReportQueryRequest(),
            GeneratedSql = execution.GeneratedSql,
            Parameters = string.IsNullOrWhiteSpace(execution.ParametersJson)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(execution.ParametersJson, JsonSerialization.Options),
            Result = string.IsNullOrWhiteSpace(execution.ResultJson)
                ? null
                : JsonSerializer.Deserialize<QueryExecutionResult>(execution.ResultJson, JsonSerialization.Options),
            RowCount = execution.RowCount,
            ErrorMessage = execution.ErrorMessage,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            CreatedAt = execution.CreatedAt
        };
    }
}
