using ReportingPlatform.Application.DTOs.Reports;

namespace ReportingPlatform.Application.Interfaces;

public interface IReportExecutionService
{
    Task<Guid> SubmitAsync(RunReportRequest request, CancellationToken cancellationToken = default);

    Task<ReportExecutionDto?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task ProcessExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
}
