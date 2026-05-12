using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.QueryPlanning;

namespace ReportingPlatform.Application.Interfaces;

public interface IQueryPlanBuilder
{
    Task<QueryPlan> BuildAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken = default);
}
