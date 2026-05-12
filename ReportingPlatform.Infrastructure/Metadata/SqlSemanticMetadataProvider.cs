using Dapper;
using ReportingPlatform.Application.DTOs;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Infrastructure.Persistence;

namespace ReportingPlatform.Infrastructure.Metadata;

public sealed class SqlSemanticMetadataProvider : ISemanticMetadataProvider
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlSemanticMetadataProvider(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ReportingEntityDto>> GetEntitiesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                EntityId,
                EntityKey,
                DisplayName,
                EntityName,
                EntityType,
                IsScopeEntity,
                IsActive
            FROM rpt.ReportingEntities
            WHERE IsActive = 1
            ORDER BY DisplayName;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var entities = await connection.QueryAsync<ReportingEntityDto>(command);

        return entities.AsList();
    }

    public async Task<ReportingEntityDto?> GetEntityAsync(string entityKey, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                EntityId,
                EntityKey,
                DisplayName,
                EntityName,
                EntityType,
                IsScopeEntity,
                IsActive
            FROM rpt.ReportingEntities
            WHERE IsActive = 1
                AND EntityKey = @EntityKey;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new { EntityKey = entityKey },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ReportingEntityDto>(command);
    }

    public async Task<IReadOnlyList<ReportingFieldDto>> GetFieldsByEntityKeyAsync(string entityKey, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                f.FieldId,
                f.EntityId,
                e.EntityKey,
                f.FieldKey,
                e.EntityKey + '.' + f.FieldKey AS SemanticKey,
                f.DisplayName,
                f.DataType,
                f.FieldRole,
                f.IsSystemFilter,
                f.IsFilterable,
                f.IsGroupable,
                f.IsSensitive,
                f.IsDefault,
                f.IsIdentifier,
                PARSENAME(e.EntityName, 2) AS PhysicalSchemaName,
                PARSENAME(e.EntityName, 1) AS PhysicalTableName,
                pc.PhysicalColumnName,
                CAST(NULL AS nvarchar(max)) AS ExpressionSql
            FROM rpt.ReportingFields f
            INNER JOIN rpt.ReportingEntities e
                ON e.EntityId = f.EntityId
            OUTER APPLY
            (
                SELECT TOP (1)
                    c.name AS PhysicalColumnName
                FROM sys.schemas s
                INNER JOIN sys.tables t
                    ON t.schema_id = s.schema_id
                INNER JOIN sys.columns c
                    ON c.object_id = t.object_id
                WHERE s.name = PARSENAME(e.EntityName, 2)
                    AND t.name = PARSENAME(e.EntityName, 1)
                    AND LOWER(REPLACE(c.name, '_', '')) = LOWER(REPLACE(f.FieldKey, '_', ''))
                ORDER BY c.column_id
            ) pc
            WHERE e.EntityKey = @EntityKey
                AND e.IsActive = 1
                AND f.IsActive = 1
            ORDER BY f.DisplayName;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new { EntityKey = entityKey },
            cancellationToken: cancellationToken);
        var fields = await connection.QueryAsync<ReportingFieldDto>(command);

        return fields.AsList();
    }

    public async Task<IReadOnlyList<ReportingRelationshipDto>> GetRelationshipsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                r.RelationshipId,
                pe.EntityKey AS ParentEntityKey,
                pe.DisplayName AS ParentEntityName,
                pf.FieldKey AS ParentFieldKey,
                pf.DisplayName AS ParentFieldName,
                ce.EntityKey AS ChildEntityKey,
                ce.DisplayName AS ChildEntityName,
                cf.FieldKey AS ChildFieldKey,
                cf.DisplayName AS ChildFieldName,
                r.JoinType,
                r.Cardinality,
                r.Direction,
                r.IsRequired
            FROM rpt.ReportingRelationships r
            INNER JOIN rpt.ReportingEntities pe
                ON pe.EntityId = r.ParentEntityId
            INNER JOIN rpt.ReportingFields pf
                ON pf.FieldId = r.ParentFieldId
            INNER JOIN rpt.ReportingEntities ce
                ON ce.EntityId = r.ChildEntityId
            INNER JOIN rpt.ReportingFields cf
                ON cf.FieldId = r.ChildFieldId
            WHERE r.IsActive = 1
                AND pe.IsActive = 1
                AND ce.IsActive = 1
                AND pf.IsActive = 1
                AND cf.IsActive = 1
            ORDER BY pe.DisplayName, ce.DisplayName, r.RelationshipId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var relationships = await connection.QueryAsync<ReportingRelationshipDto>(command);

        return relationships.AsList();
    }
}
