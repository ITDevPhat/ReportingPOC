using System.Collections.Concurrent;

namespace ReportingPlatform.Api.Middleware;

public sealed class SimpleRateLimitingMiddleware
{
    private static readonly ConcurrentDictionary<string, RateLimitWindow> Windows = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] HeavyPathPrefixes =
    [
        "/api/report-query/execute",
        "/api/report-executions/run",
        "/api/sql-generator/generate",
        "/api/report-exports"
    ];

    private readonly RequestDelegate _next;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;

    public SimpleRateLimitingMiddleware(
        RequestDelegate next,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _next = next;
        _permitLimit = int.TryParse(configuration["RateLimiting:PermitLimit"], out var permitLimit)
            ? permitLimit
            : 100;
        var windowSeconds = int.TryParse(configuration["RateLimiting:WindowSeconds"], out var configuredWindowSeconds)
            ? configuredWindowSeconds
            : 60;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var key = $"{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        var now = DateTimeOffset.UtcNow;
        var window = Windows.GetOrAdd(key, _ => new RateLimitWindow(now));

        lock (window)
        {
            if (now - window.StartedAt >= _window)
            {
                window.StartedAt = now;
                window.Count = 0;
            }

            window.Count++;
            if (window.Count > _permitLimit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldLimit(PathString path)
    {
        var value = path.Value ?? string.Empty;

        return HeavyPathPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RateLimitWindow
    {
        public RateLimitWindow(DateTimeOffset startedAt)
        {
            StartedAt = startedAt;
        }

        public DateTimeOffset StartedAt { get; set; }
        public int Count { get; set; }
    }
}
