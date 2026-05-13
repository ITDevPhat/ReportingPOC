using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.DTOs.Reports;

public sealed class RunReportRequest
{
    public ReportQueryRequest? Query { get; set; }
    public Guid? TemplateId { get; set; }
    public string? CreatedBy { get; set; }
}
