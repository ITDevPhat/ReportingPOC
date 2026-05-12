using ReportingPlatform.Domain.Enums;

namespace ReportingPlatform.Domain.Query;

public sealed class QueryMetricDefinition
{
    public string MetricKey { get; init; } = string.Empty;
    public MetricFunction Function { get; init; }
    public string Field { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
}
