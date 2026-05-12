namespace ReportingPlatform.Domain.Metadata;

public sealed class RelationshipEdge
{
    public int RelationshipId { get; set; }
    public string ParentEntityKey { get; set; } = string.Empty;
    public string ParentEntityName { get; set; } = string.Empty;
    public string ParentFieldKey { get; set; } = string.Empty;
    public string ChildEntityKey { get; set; } = string.Empty;
    public string ChildEntityName { get; set; } = string.Empty;
    public string ChildFieldKey { get; set; } = string.Empty;
    public string JoinType { get; set; } = string.Empty;
    public string Cardinality { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    public string EdgeKey => $"{ParentEntityKey}->{ChildEntityKey}";
}
