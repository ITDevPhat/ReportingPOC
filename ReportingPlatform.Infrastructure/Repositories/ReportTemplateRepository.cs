using System.Text.Json;
using Dapper;
using ReportingPlatform.Application.DTOs.Reports;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.Reports;
using ReportingPlatform.Infrastructure.Persistence;

namespace ReportingPlatform.Infrastructure.Repositories;

public sealed class ReportTemplateRepository : IReportTemplateRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ReportTemplateRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreateAsync(
        CreateReportTemplateRequest request,
        string? createdBy,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO rpt.ReportTemplates
            (
                TemplateId,
                TemplateKey,
                Name,
                Description,
                BaseEntityKey,
                QueryJson,
                CreatedBy
            )
            VALUES
            (
                @TemplateId,
                @TemplateKey,
                @Name,
                @Description,
                @BaseEntityKey,
                @QueryJson,
                @CreatedBy
            );
            """;

        var templateId = Guid.NewGuid();
        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                TemplateId = templateId,
                request.TemplateKey,
                request.Name,
                request.Description,
                BaseEntityKey = request.Query.BaseEntity,
                QueryJson = JsonSerializer.Serialize(request.Query, JsonSerialization.Options),
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        return templateId;
    }

    public async Task UpdateAsync(
        Guid templateId,
        UpdateReportTemplateRequest request,
        string? updatedBy,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE rpt.ReportTemplates
            SET
                Name = @Name,
                Description = @Description,
                BaseEntityKey = @BaseEntityKey,
                QueryJson = @QueryJson,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedBy = @UpdatedBy
            WHERE TemplateId = @TemplateId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                TemplateId = templateId,
                request.Name,
                request.Description,
                BaseEntityKey = request.Query.BaseEntity,
                QueryJson = JsonSerializer.Serialize(request.Query, JsonSerialization.Options),
                request.IsActive,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<ReportTemplateDto?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM rpt.ReportTemplates
            WHERE TemplateId = @TemplateId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var template = await connection.QuerySingleOrDefaultAsync<ReportTemplate>(
            new CommandDefinition(sql, new { TemplateId = templateId }, cancellationToken: cancellationToken));

        return template is null ? null : Map(template);
    }

    public async Task<ReportTemplateDto?> GetByKeyAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM rpt.ReportTemplates
            WHERE TemplateKey = @TemplateKey;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var template = await connection.QuerySingleOrDefaultAsync<ReportTemplate>(
            new CommandDefinition(sql, new { TemplateKey = templateKey }, cancellationToken: cancellationToken));

        return template is null ? null : Map(template);
    }

    public async Task<IReadOnlyList<ReportTemplateDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM rpt.ReportTemplates
            ORDER BY CreatedAt DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var templates = await connection.QueryAsync<ReportTemplate>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return templates.Select(Map).ToList();
    }

    public async Task DeactivateAsync(Guid templateId, string? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE rpt.ReportTemplates
            SET
                IsActive = 0,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedBy = @UpdatedBy
            WHERE TemplateId = @TemplateId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { TemplateId = templateId, UpdatedBy = updatedBy }, cancellationToken: cancellationToken));
    }

    private static ReportTemplateDto Map(ReportTemplate template)
    {
        return new ReportTemplateDto
        {
            TemplateId = template.TemplateId,
            TemplateKey = template.TemplateKey,
            Name = template.Name,
            Description = template.Description,
            BaseEntityKey = template.BaseEntityKey,
            Query = JsonSerializer.Deserialize<ReportQueryRequest>(template.QueryJson, JsonSerialization.Options) ?? new ReportQueryRequest(),
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
