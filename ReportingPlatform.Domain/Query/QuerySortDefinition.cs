using ReportingPlatform.Domain.Enums;

namespace ReportingPlatform.Domain.Query;

public sealed class QuerySortDefinition
{
    public string Field { get; init; } = string.Empty;
    public SortDirection Direction { get; init; }
}
