using Microsoft.Extensions.Caching.Memory;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Infrastructure.Caching;

public sealed class MemoryQueryResultCache : IQueryResultCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryQueryResultCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<QueryExecutionResult?> GetAsync(string queryHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.TryGetValue(GetCacheKey(queryHash), out QueryExecutionResult? result);

        return Task.FromResult(result);
    }

    public Task SetAsync(
        string queryHash,
        QueryExecutionResult result,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.Set(GetCacheKey(queryHash), result, ttl);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string queryHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.Remove(GetCacheKey(queryHash));

        return Task.CompletedTask;
    }

    private static string GetCacheKey(string queryHash)
    {
        return $"report-query:{queryHash}";
    }
}
