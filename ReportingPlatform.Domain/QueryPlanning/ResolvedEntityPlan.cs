namespace ReportingPlatform.Domain.QueryPlanning;

public sealed class ResolvedEntityPlan
{
    public string EntityKey { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}
