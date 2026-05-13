using ReportingPlatform.Domain.Exports;

namespace ReportingPlatform.Application.DTOs.Exports;

public sealed class CreateExportRequest
{
    public ExportFormat Format { get; set; }
    public string FileName { get; set; } = string.Empty;
}
