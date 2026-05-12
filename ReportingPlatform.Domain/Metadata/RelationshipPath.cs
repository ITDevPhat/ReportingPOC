namespace ReportingPlatform.Domain.Metadata;

public sealed class RelationshipPath
{
    public string BaseEntityKey { get; set; } = string.Empty;
    public string TargetEntityKey { get; set; } = string.Empty;
    public List<JoinPlan> Joins { get; set; } = [];

    public bool HasPath => Joins.Count > 0;

    public int Depth => Joins.Count;
}
