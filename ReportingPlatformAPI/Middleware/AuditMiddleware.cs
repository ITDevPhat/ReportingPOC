using System.Diagnostics;
using System.Text.Json;
using ReportingPlatform.Application.DTOs.Reports;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.Reports;

namespace ReportingPlatform.Api.Middleware;

public sealed class AuditMiddleware
{
    private static readonly string[] AuditedPathPrefixes =
    [
        "/api/report-query",
        "/api/report-executions",
        "/api/report-templates",
        "/api/sql-generator"
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuditLogRepository auditLogRepository,
        IQueryHashService queryHashService)
    {
        var shouldAudit = ShouldAudit(context.Request.Path);
        var stopwatch = shouldAudit ? Stopwatch.StartNew() : null;
        var auditContext = shouldAudit
            ? await CreateRequestAuditContextAsync(context, queryHashService)
            : new RequestAuditContext();

        try
        {
            await _next(context);
        }
        finally
        {
            if (shouldAudit && stopwatch is not null)
            {
                stopwatch.Stop();

                try
                {
                    var auditLog = CreateAuditLog(context, auditContext, stopwatch.ElapsedMilliseconds);
                    await auditLogRepository.WriteAsync(auditLog, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Audit log write failed for {RequestPath}.", context.Request.Path.Value);
                }
            }
        }
    }

    private static bool ShouldAudit(PathString path)
    {
        var value = path.Value ?? string.Empty;

        return AuditedPathPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<RequestAuditContext> CreateRequestAuditContextAsync(
        HttpContext context,
        IQueryHashService queryHashService)
    {
        var auditContext = new RequestAuditContext();

        try
        {
            if (!HttpMethods.IsPost(context.Request.Method)
                || context.Request.ContentLength is null or 0
                || context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) != true)
            {
                return auditContext;
            }

            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                return auditContext;
            }

            var path = context.Request.Path.Value ?? string.Empty;
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (path.StartsWith("/api/report-query", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/api/sql-generator", StringComparison.OrdinalIgnoreCase))
            {
                var request = JsonSerializer.Deserialize<ReportQueryRequest>(body, jsonOptions);
                if (request is not null)
                {
                    auditContext.QueryHash = queryHashService.ComputeHash(request);
                }
            }
            else if (path.StartsWith("/api/report-executions/run", StringComparison.OrdinalIgnoreCase))
            {
                var request = JsonSerializer.Deserialize<RunReportRequest>(body, jsonOptions);
                auditContext.TemplateId = request?.TemplateId;
                if (request?.Query is not null)
                {
                    auditContext.QueryHash = queryHashService.ComputeHash(request.Query);
                }
            }
        }
        catch
        {
            // Audit enrichment is best-effort and must not affect the request.
        }

        return auditContext;
    }

    private static ReportAuditLog CreateAuditLog(
        HttpContext context,
        RequestAuditContext auditContext,
        long durationMs)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Path.Value ?? string.Empty;
        var (entityType, entityId, executionId, templateId) = ResolveEntity(path);

        return new ReportAuditLog
        {
            AuditLogId = Guid.NewGuid(),
            EventType = "ApiRequest",
            EntityType = entityType,
            EntityId = entityId,
            UserId = GetUserId(context),
            RequestPath = path,
            RequestMethod = request.Method,
            QueryHash = auditContext.QueryHash,
            ExecutionId = executionId,
            TemplateId = templateId ?? auditContext.TemplateId,
            Status = response.StatusCode.ToString(),
            DurationMs = durationMs,
            MetadataJson = JsonSerializer.Serialize(new
            {
                response.StatusCode,
                request.QueryString.Value
            })
        };
    }

    private static string? GetUserId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.Identity.Name;
        }

        return context.Request.Headers.TryGetValue("X-User-Id", out var userId)
            ? userId.ToString()
            : null;
    }

    private static (string? EntityType, string? EntityId, Guid? ExecutionId, Guid? TemplateId) ResolveEntity(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var guid = segments.Select(segment => Guid.TryParse(segment, out var value) ? value : (Guid?)null)
            .FirstOrDefault(value => value is not null);

        if (segments.Contains("report-executions", StringComparer.OrdinalIgnoreCase))
        {
            return ("ReportExecution", guid?.ToString(), guid, null);
        }

        if (segments.Contains("report-templates", StringComparer.OrdinalIgnoreCase))
        {
            return ("ReportTemplate", guid?.ToString(), null, guid);
        }

        if (segments.Contains("report-query", StringComparer.OrdinalIgnoreCase))
        {
            return ("ReportQuery", null, null, null);
        }

        if (segments.Contains("sql-generator", StringComparer.OrdinalIgnoreCase))
        {
            return ("SqlGenerator", null, null, null);
        }

        return (null, null, null, null);
    }

    private sealed class RequestAuditContext
    {
        public string? QueryHash { get; set; }
        public Guid? TemplateId { get; set; }
    }
}
