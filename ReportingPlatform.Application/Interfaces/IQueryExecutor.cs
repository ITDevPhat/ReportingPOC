using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Application.Interfaces;

public interface IQueryExecutor
{
    Task<QueryExecutionResult> ExecuteAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken = default);
}
