namespace ReportingPlatform.Domain.Metadata;

public sealed class JoinPathNode
{
    public string EntityKey { get; set; } = string.Empty;
    public RelationshipEdge? IncomingEdge { get; set; }
    public JoinPathNode? PreviousNode { get; set; }
}
