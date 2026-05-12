namespace ReportingPlatform.Application.DTOs;

public sealed class ResolveRelationshipPathsRequest
{
    public string BaseEntityKey { get; set; } = string.Empty;
    public List<string> TargetEntityKeys { get; set; } = [];
}
