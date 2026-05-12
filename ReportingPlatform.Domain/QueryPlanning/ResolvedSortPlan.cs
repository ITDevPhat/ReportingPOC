namespace ReportingPlatform.Domain.QueryPlanning;

public sealed class ResolvedSortPlan
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool IsMetricAlias { get; set; }
    public bool IsSelectField { get; set; }
}
