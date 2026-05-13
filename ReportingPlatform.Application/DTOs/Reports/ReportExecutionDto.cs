using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Application.DTOs.Reports;

public sealed class ReportExecutionDto
{
    public Guid ExecutionId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Status { get; set; } = string.Empty;
    public ReportQueryRequest Query { get; set; } = new();
    public string? GeneratedSql { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public QueryExecutionResult? Result { get; set; }
    public int? RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
