using ReportingPlatform.Domain.Metadata;

namespace ReportingPlatform.Application.Interfaces;

public interface IRelationshipGraphProvider
{
    Task<IReadOnlyList<RelationshipEdge>> GetRelationshipGraphAsync(CancellationToken cancellationToken = default);
}
