using ReportingPlatform.Application.DTOs;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Metadata;

namespace ReportingPlatform.Application.Resolvers;

public sealed class RelationshipGraphProvider : IRelationshipGraphProvider
{
    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static IReadOnlyList<RelationshipEdge>? cachedGraph;

    private readonly ISemanticMetadataProvider _metadataProvider;

    public RelationshipGraphProvider(ISemanticMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    public async Task<IReadOnlyList<RelationshipEdge>> GetRelationshipGraphAsync(CancellationToken cancellationToken = default)
    {
        if (cachedGraph is not null)
        {
            return cachedGraph;
        }

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (cachedGraph is not null)
            {
                return cachedGraph;
            }

            var relationships = await _metadataProvider.GetRelationshipsAsync(cancellationToken);
            cachedGraph = relationships
                .Select(MapEdge)
                .GroupBy(x => x.EdgeKey, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();

            return cachedGraph;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private static RelationshipEdge MapEdge(ReportingRelationshipDto relationship)
    {
        return new RelationshipEdge
        {
            RelationshipId = relationship.RelationshipId,
            ParentEntityKey = relationship.ParentEntityKey,
            ParentEntityName = relationship.ParentEntityName,
            ParentFieldKey = relationship.ParentFieldKey,
            ChildEntityKey = relationship.ChildEntityKey,
            ChildEntityName = relationship.ChildEntityName,
            ChildFieldKey = relationship.ChildFieldKey,
            JoinType = relationship.JoinType,
            Cardinality = relationship.Cardinality,
            Direction = relationship.Direction,
            IsRequired = relationship.IsRequired
        };
    }
}
