using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Infrastructure.Exports;

public sealed class ExcelReportExporter
{
    public Task<Stream> ExportAsync(QueryExecutionResult result, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Excel export is not configured in this build. Install and wire a supported Excel writer such as Telerik SpreadsheetStreaming to enable it.");
    }
}
