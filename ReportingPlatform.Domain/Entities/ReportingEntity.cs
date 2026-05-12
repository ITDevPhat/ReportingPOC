namespace ReportingPlatform.Domain.Entities;

public sealed class ReportingEntity
{
    public int EntityId { get; set; }
    public string EntityKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public bool IsScopeEntity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
