using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.DTOs.Reports;

public sealed class UpdateReportTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportQueryRequest Query { get; set; } = new();
    public bool IsActive { get; set; }
}
