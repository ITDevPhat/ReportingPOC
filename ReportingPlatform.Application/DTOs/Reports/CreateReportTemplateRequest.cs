using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.DTOs.Reports;

public sealed class CreateReportTemplateRequest
{
    public string TemplateKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportQueryRequest Query { get; set; } = new();
}
