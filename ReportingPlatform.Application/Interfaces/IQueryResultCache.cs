using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Application.Interfaces;

public interface IQueryResultCache
{
    Task<QueryExecutionResult?> GetAsync(string queryHash, CancellationToken cancellationToken = default);

    Task SetAsync(string queryHash, QueryExecutionResult result, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveAsync(string queryHash, CancellationToken cancellationToken = default);
}
