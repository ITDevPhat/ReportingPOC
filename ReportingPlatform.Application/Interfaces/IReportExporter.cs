using ReportingPlatform.Domain.Exports;
using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Application.Interfaces;

public interface IReportExporter
{
    Task<ExportResult> ExportAsync(
        QueryExecutionResult result,
        ExportFormat format,
        string fileName,
        CancellationToken cancellationToken = default);
}
