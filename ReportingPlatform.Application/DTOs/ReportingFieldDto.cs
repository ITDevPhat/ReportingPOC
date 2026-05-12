namespace ReportingPlatform.Application.DTOs;

public sealed class ReportingFieldDto
{
    public int FieldId { get; set; }
    public int EntityId { get; set; }
    public string EntityKey { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string SemanticKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string FieldRole { get; set; } = string.Empty;
    public bool IsSystemFilter { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsGroupable { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsDefault { get; set; }
    public bool IsIdentifier { get; set; }
    public string? PhysicalSchemaName { get; set; }
    public string? PhysicalTableName { get; set; }
    public string? PhysicalColumnName { get; set; }
    public string? ExpressionSql { get; set; }
}
