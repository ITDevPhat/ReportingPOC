namespace ReportingPlatform.Application.DTOs;

public sealed class ReportingEntityDto
{
    public int EntityId { get; set; }
    public string EntityKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public bool IsScopeEntity { get; set; }
    public bool IsActive { get; set; }
}
