namespace ReportingPlatform.Domain.QueryPlanning;

public sealed class ResolvedSelectFieldPlan
{
    public string SemanticKey { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string PhysicalColumnName { get; set; } = string.Empty;
    public string SqlQualifiedName { get; set; } = string.Empty;
    public string OutputAlias { get; set; } = string.Empty;
}
