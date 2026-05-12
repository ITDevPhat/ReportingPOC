using ReportingPlatform.Domain.Metadata;

namespace ReportingPlatform.Domain.QueryPlanning;

public sealed class QueryPlan
{
    public string BaseEntityKey { get; set; } = string.Empty;
    public ResolvedEntityPlan BaseEntity { get; set; } = new();
    public List<ResolvedSelectFieldPlan> SelectFields { get; set; } = [];
    public List<ResolvedMetricPlan> Metrics { get; set; } = [];
    public List<ResolvedFilterPlan> Filters { get; set; } = [];
    public List<ResolvedGroupByPlan> GroupByFields { get; set; } = [];
    public List<ResolvedSortPlan> Sorts { get; set; } = [];
    public List<JoinPlan> Joins { get; set; } = [];
    public int Limit { get; set; }
}
