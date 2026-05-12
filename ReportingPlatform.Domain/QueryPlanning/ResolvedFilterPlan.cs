namespace ReportingPlatform.Domain.QueryPlanning;

public sealed class ResolvedFilterPlan
{
    public string FieldSemanticKey { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string PhysicalColumnName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object? Value { get; set; }
}
