namespace ReportingPlatform.Domain.Metadata;

public sealed class JoinPlan
{
    public string ParentEntityKey { get; set; } = string.Empty;
    public string ParentAlias { get; set; } = string.Empty;
    public string ParentPhysicalSchemaName { get; set; } = string.Empty;
    public string ParentPhysicalTableName { get; set; } = string.Empty;
    public string ParentPhysicalColumnName { get; set; } = string.Empty;
    public string ChildEntityKey { get; set; } = string.Empty;
    public string ChildAlias { get; set; } = string.Empty;
    public string ChildPhysicalSchemaName { get; set; } = string.Empty;
    public string ChildPhysicalTableName { get; set; } = string.Empty;
    public string ChildPhysicalColumnName { get; set; } = string.Empty;
    public string ParentFieldKey { get; set; } = string.Empty;
    public string ChildFieldKey { get; set; } = string.Empty;
    public string JoinType { get; set; } = string.Empty;
    public string Cardinality { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    public string JoinSignature =>
        $"{ParentEntityKey}.{ParentFieldKey}->{ChildEntityKey}.{ChildFieldKey}";
}
