namespace ReportingPlatform.Domain.Query;

public sealed class ReportQueryRequest
{
    public string BaseEntity { get; init; } = string.Empty;
    public List<string> SelectFields { get; init; } = [];
    public List<QueryMetricDefinition> Metrics { get; init; } = [];
    public List<QueryFilterDefinition> Filters { get; init; } = [];
    public List<string> GroupBy { get; init; } = [];
    public List<QuerySortDefinition> Sort { get; init; } = [];
    public int Limit { get; init; } = 100;
}
