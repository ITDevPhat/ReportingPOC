namespace ReportingPlatform.Domain.QueryPlanning;

public sealed class ResolvedGroupByPlan
{
    public string SemanticKey { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string SqlQualifiedName { get; set; } = string.Empty;
}
