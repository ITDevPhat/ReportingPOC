namespace ReportingPlatform.Domain.Metadata;

public sealed class ResolvedField
{
    public string SemanticKey { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string EntityKey { get; set; } = string.Empty;
    public string EntityDisplayName { get; set; } = string.Empty;
    public int FieldId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldDisplayName { get; set; } = string.Empty;
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

    public bool IsPhysicalColumn =>
        !string.IsNullOrWhiteSpace(PhysicalSchemaName)
        && !string.IsNullOrWhiteSpace(PhysicalTableName)
        && !string.IsNullOrWhiteSpace(PhysicalColumnName);

    public string SqlQualifiedName
    {
        get
        {
            if (!IsPhysicalColumn)
            {
                throw new InvalidOperationException(
                    $"Semantic field '{SemanticKey}' does not resolve to a physical database column.");
            }

            return $"{QuoteIdentifier(PhysicalSchemaName!)}.{QuoteIdentifier(PhysicalTableName!)}.{QuoteIdentifier(PhysicalColumnName!)}";
        }
    }

    private static string QuoteIdentifier(string value)
    {
        return $"[{value.Replace("]", "]]")}]";
    }
}
