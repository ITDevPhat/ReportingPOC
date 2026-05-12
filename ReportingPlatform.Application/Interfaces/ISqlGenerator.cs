using ReportingPlatform.Domain.QueryPlanning;
using ReportingPlatform.Domain.SqlGeneration;

namespace ReportingPlatform.Application.Interfaces;

public interface ISqlGenerator
{
    Task<GeneratedSqlResult> GenerateAsync(
        QueryPlan queryPlan,
        CancellationToken cancellationToken = default);
}
