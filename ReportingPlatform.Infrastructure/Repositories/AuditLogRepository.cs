using Dapper;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Reports;
using ReportingPlatform.Infrastructure.Persistence;

namespace ReportingPlatform.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AuditLogRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task WriteAsync(ReportAuditLog log, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO rpt.ReportAuditLogs
            (
                AuditLogId,
                EventType,
                EntityType,
                EntityId,
                UserId,
                RequestPath,
                RequestMethod,
                QueryHash,
                ExecutionId,
                TemplateId,
                Status,
                DurationMs,
                MetadataJson
            )
            VALUES
            (
                @AuditLogId,
                @EventType,
                @EntityType,
                @EntityId,
                @UserId,
                @RequestPath,
                @RequestMethod,
                @QueryHash,
                @ExecutionId,
                @TemplateId,
                @Status,
                @DurationMs,
                @MetadataJson
            );
            """;

        if (log.AuditLogId == Guid.Empty)
        {
            log.AuditLogId = Guid.NewGuid();
        }

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, log, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ReportAuditLog>> ListAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? eventType = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (500)
                AuditLogId,
                EventType,
                EntityType,
                EntityId,
                UserId,
                RequestPath,
                RequestMethod,
                QueryHash,
                ExecutionId,
                TemplateId,
                Status,
                DurationMs,
                MetadataJson,
                CreatedAt
            FROM rpt.ReportAuditLogs
            WHERE (@From IS NULL OR CreatedAt >= @From)
              AND (@To IS NULL OR CreatedAt <= @To)
              AND (@EventType IS NULL OR EventType = @EventType)
            ORDER BY CreatedAt DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var logs = await connection.QueryAsync<ReportAuditLog>(
            new CommandDefinition(
                sql,
                new { From = from, To = to, EventType = eventType },
                cancellationToken: cancellationToken));

        return logs.AsList();
    }
}
