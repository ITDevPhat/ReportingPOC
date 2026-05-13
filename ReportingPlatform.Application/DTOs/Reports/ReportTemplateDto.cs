using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.DTOs.Reports;

public sealed class ReportTemplateDto
{
    public Guid TemplateId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BaseEntityKey { get; set; } = string.Empty;
    public ReportQueryRequest Query { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
