namespace ReportingPlatform.Domain.Entities;

public sealed class ReportingRelationship
{
    public int RelationshipId { get; set; }
    public int ParentEntityId { get; set; }
    public int ParentFieldId { get; set; }
    public int ChildEntityId { get; set; }
    public int ChildFieldId { get; set; }
    public string JoinType { get; set; } = string.Empty;
    public string Cardinality { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
