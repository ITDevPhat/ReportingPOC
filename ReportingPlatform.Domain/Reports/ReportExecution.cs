namespace ReportingPlatform.Domain.Reports;

public sealed class ReportExecution
{
    public Guid ExecutionId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string QueryJson { get; set; } = string.Empty;
    public string? GeneratedSql { get; set; }
    public string? ParametersJson { get; set; }
    public string? ResultJson { get; set; }
    public int? RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
