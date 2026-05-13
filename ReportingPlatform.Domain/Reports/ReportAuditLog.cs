namespace ReportingPlatform.Domain.Reports;

public sealed class ReportAuditLog
{
    public Guid AuditLogId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string? QueryHash { get; set; }
    public Guid? ExecutionId { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Status { get; set; }
    public long? DurationMs { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
