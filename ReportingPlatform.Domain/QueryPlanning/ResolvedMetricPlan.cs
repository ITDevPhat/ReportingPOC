namespace ReportingPlatform.Domain.QueryPlanning;

public sealed class ResolvedMetricPlan
{
    public string MetricKey { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
    public string FieldSemanticKey { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string PhysicalColumnName { get; set; } = string.Empty;
    public string SqlQualifiedName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}
