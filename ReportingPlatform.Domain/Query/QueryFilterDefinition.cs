using ReportingPlatform.Domain.Enums;

namespace ReportingPlatform.Domain.Query;

public sealed class QueryFilterDefinition
{
    public string Field { get; init; } = string.Empty;
    public FilterOperator Operator { get; init; }
    public object? Value { get; init; }
}
