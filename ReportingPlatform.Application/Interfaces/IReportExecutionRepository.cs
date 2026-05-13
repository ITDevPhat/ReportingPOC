using ReportingPlatform.Application.DTOs.Reports;
using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Application.Interfaces;

public interface IReportExecutionRepository
{
    Task<Guid> CreatePendingAsync(RunReportRequest request, CancellationToken cancellationToken = default);

    Task MarkProcessingAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(Guid executionId, QueryExecutionResult result, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid executionId, string errorMessage, CancellationToken cancellationToken = default);

    Task<ReportExecutionDto?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportExecutionDto>> ListAsync(Guid? templateId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetPendingExecutionIdsAsync(int take = 10, CancellationToken cancellationToken = default);
}
