using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exports;
using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Infrastructure.Exports;

public sealed class ReportExportService : IReportExporter
{
    private readonly IArtifactStorage _artifactStorage;
    private readonly CsvReportExporter _csvExporter;
    private readonly ExcelReportExporter _excelExporter;
    private readonly ILogger<ReportExportService> _logger;

    public ReportExportService(
        IArtifactStorage artifactStorage,
        CsvReportExporter csvExporter,
        ExcelReportExporter excelExporter,
        ILogger<ReportExportService> logger)
    {
        _artifactStorage = artifactStorage;
        _csvExporter = csvExporter;
        _excelExporter = excelExporter;
        _logger = logger;
    }

    public async Task<ExportResult> ExportAsync(
        QueryExecutionResult result,
        ExportFormat format,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var (contentType, extension, content) = format switch
        {
            ExportFormat.Csv => (
                "text/csv",
                ".csv",
                await _csvExporter.ExportAsync(result, cancellationToken)),
            ExportFormat.Excel => (
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xlsx",
                await _excelExporter.ExportAsync(result, cancellationToken)),
            ExportFormat.Pdf => throw new NotSupportedException("PDF export is deferred for this phase."),
            _ => throw new NotSupportedException($"Export format '{format}' is not supported.")
        };

        await using (content)
        {
            var safeFileName = EnsureExtension(fileName, extension);
            var exportResult = await _artifactStorage.SaveAsync(safeFileName, contentType, content, cancellationToken);
            _logger.LogInformation(
                "Created report export {ArtifactKey} with {SizeBytes} bytes.",
                exportResult.ArtifactKey,
                exportResult.SizeBytes);

            return exportResult;
        }
    }

    private static string EnsureExtension(string fileName, string extension)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = $"report-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        }

        return Path.HasExtension(safeFileName)
            ? safeFileName
            : safeFileName + extension;
    }
}
