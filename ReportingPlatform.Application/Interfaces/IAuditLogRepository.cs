using ReportingPlatform.Domain.Reports;

namespace ReportingPlatform.Application.Interfaces;

public interface IAuditLogRepository
{
    Task WriteAsync(ReportAuditLog log, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportAuditLog>> ListAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? eventType = null,
        CancellationToken cancellationToken = default);
}
