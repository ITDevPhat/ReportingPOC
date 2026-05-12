namespace ReportingPlatform.Domain.Entities;

public sealed class ReportingField
{
    public int FieldId { get; set; }
    public int EntityId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string FieldRole { get; set; } = string.Empty;
    public bool IsSystemFilter { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsGroupable { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPossibleForHeader_Footer { get; set; }
    public bool IsIdentifier { get; set; }
    public string? PhysicalSchemaName { get; set; }
    public string? PhysicalTableName { get; set; }
    public string? PhysicalColumnName { get; set; }
    public string? ExpressionSql { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
