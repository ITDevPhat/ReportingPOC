using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Metadata;

namespace ReportingPlatform.Application.Resolvers;

public sealed class RelationshipResolver : IRelationshipResolver
{
    private readonly IRelationshipGraphProvider _relationshipGraphProvider;
    private readonly ILogger<RelationshipResolver> _logger;

    public RelationshipResolver(
        IRelationshipGraphProvider relationshipGraphProvider,
        ILogger<RelationshipResolver> logger)
    {
        _relationshipGraphProvider = relationshipGraphProvider;
        _logger = logger;
    }

    public async Task<RelationshipPath> ResolvePathAsync(
        string baseEntityKey,
        string targetEntityKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedBaseEntityKey = NormalizeEntityKey(baseEntityKey, nameof(baseEntityKey));
        var normalizedTargetEntityKey = NormalizeEntityKey(targetEntityKey, nameof(targetEntityKey));

        if (string.Equals(normalizedBaseEntityKey, normalizedTargetEntityKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Base entity key and target entity key must be different.");
        }

        var graph = await _relationshipGraphProvider.GetRelationshipGraphAsync(cancellationToken);
        var knownEntityKeys = GetKnownEntityKeys(graph);

        if (!knownEntityKeys.Contains(normalizedBaseEntityKey) || !knownEntityKeys.Contains(normalizedTargetEntityKey))
        {
            _logger.LogWarning(
                "Relationship path requested for unknown entity pair {BaseEntityKey} -> {TargetEntityKey}.",
                normalizedBaseEntityKey,
                normalizedTargetEntityKey);

            throw new RelationshipPathNotFoundException(normalizedBaseEntityKey, normalizedTargetEntityKey);
        }

        var shortestPaths = FindShortestPaths(graph, normalizedBaseEntityKey, normalizedTargetEntityKey);

        if (shortestPaths.Count == 0)
        {
            throw new RelationshipPathNotFoundException(normalizedBaseEntityKey, normalizedTargetEntityKey);
        }

        if (shortestPaths.Count > 1)
        {
            throw new AmbiguousRelationshipPathException(normalizedBaseEntityKey, normalizedTargetEntityKey);
        }

        return new RelationshipPath
        {
            BaseEntityKey = normalizedBaseEntityKey,
            TargetEntityKey = normalizedTargetEntityKey,
            Joins = BuildJoinPlans(shortestPaths[0])
        };
    }

    public async Task<IReadOnlyList<RelationshipPath>> ResolvePathsAsync(
        string baseEntityKey,
        IEnumerable<string> targetEntityKeys,
        CancellationToken cancellationToken = default)
    {
        if (targetEntityKeys is null)
        {
            throw new ArgumentException("At least one target entity key is required.", nameof(targetEntityKeys));
        }

        var normalizedTargets = new List<string>();
        var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var targetEntityKey in targetEntityKeys)
        {
            var normalizedTarget = NormalizeEntityKey(targetEntityKey, nameof(targetEntityKeys));
            if (seenTargets.Add(normalizedTarget))
            {
                normalizedTargets.Add(normalizedTarget);
            }
        }

        if (normalizedTargets.Count == 0)
        {
            throw new ArgumentException("At least one target entity key is required.", nameof(targetEntityKeys));
        }

        var paths = new List<RelationshipPath>(normalizedTargets.Count);

        foreach (var targetEntityKey in normalizedTargets)
        {
            paths.Add(await ResolvePathAsync(baseEntityKey, targetEntityKey, cancellationToken));
        }

        return paths;
    }

    private static List<JoinPathNode> FindShortestPaths(
        IReadOnlyList<RelationshipEdge> graph,
        string baseEntityKey,
        string targetEntityKey)
    {
        var queue = new Queue<SearchState>();
        var startNode = new JoinPathNode { EntityKey = baseEntityKey };
        var visitedDepths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [baseEntityKey] = 0
        };
        var shortestPaths = new List<JoinPathNode>();
        int? shortestDepth = null;

        queue.Enqueue(new SearchState(
            startNode,
            0,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { baseEntityKey },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

        while (queue.Count > 0)
        {
            var state = queue.Dequeue();

            if (shortestDepth is not null && state.Depth > shortestDepth.Value)
            {
                continue;
            }

            if (string.Equals(state.Node.EntityKey, targetEntityKey, StringComparison.OrdinalIgnoreCase) && state.Depth > 0)
            {
                shortestDepth ??= state.Depth;

                if (state.Depth == shortestDepth.Value)
                {
                    shortestPaths.Add(state.Node);
                }

                continue;
            }

            if (shortestDepth is not null && state.Depth >= shortestDepth.Value)
            {
                continue;
            }

            foreach (var edge in GetAdjacentEdges(graph, state.Node.EntityKey))
            {
                var nextEntityKey = GetOppositeEntityKey(edge, state.Node.EntityKey);
                if (state.VisitedEntityKeys.Contains(nextEntityKey))
                {
                    continue;
                }

                var joinSignature = ToJoinPlan(edge).JoinSignature;
                if (state.JoinSignatures.Contains(joinSignature))
                {
                    continue;
                }

                var nextDepth = state.Depth + 1;
                if (visitedDepths.TryGetValue(nextEntityKey, out var knownDepth) && knownDepth < nextDepth)
                {
                    continue;
                }

                visitedDepths[nextEntityKey] = nextDepth;

                var nextNode = new JoinPathNode
                {
                    EntityKey = nextEntityKey,
                    IncomingEdge = edge,
                    PreviousNode = state.Node
                };

                var nextVisitedEntityKeys = new HashSet<string>(state.VisitedEntityKeys, StringComparer.OrdinalIgnoreCase)
                {
                    nextEntityKey
                };
                var nextJoinSignatures = new HashSet<string>(state.JoinSignatures, StringComparer.OrdinalIgnoreCase)
                {
                    joinSignature
                };

                queue.Enqueue(new SearchState(nextNode, nextDepth, nextVisitedEntityKeys, nextJoinSignatures));
            }
        }

        return shortestPaths;
    }

    private static IReadOnlyList<RelationshipEdge> GetAdjacentEdges(
        IReadOnlyList<RelationshipEdge> graph,
        string entityKey)
    {
        return graph
            .Where(edge =>
                string.Equals(edge.ParentEntityKey, entityKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(edge.ChildEntityKey, entityKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(edge => edge.RelationshipId)
            .ToList();
    }

    private static string GetOppositeEntityKey(RelationshipEdge edge, string entityKey)
    {
        if (string.Equals(edge.ParentEntityKey, entityKey, StringComparison.OrdinalIgnoreCase))
        {
            return edge.ChildEntityKey;
        }

        return edge.ParentEntityKey;
    }

    private static List<JoinPlan> BuildJoinPlans(JoinPathNode targetNode)
    {
        var edges = new List<RelationshipEdge>();
        var currentNode = targetNode;

        while (currentNode.IncomingEdge is not null)
        {
            edges.Add(currentNode.IncomingEdge);
            currentNode = currentNode.PreviousNode!;
        }

        edges.Reverse();

        return edges.Select(ToJoinPlan).ToList();
    }

    private static JoinPlan ToJoinPlan(RelationshipEdge edge)
    {
        return new JoinPlan
        {
            ParentEntityKey = edge.ParentEntityKey,
            ChildEntityKey = edge.ChildEntityKey,
            ParentFieldKey = edge.ParentFieldKey,
            ChildFieldKey = edge.ChildFieldKey,
            JoinType = edge.JoinType,
            Cardinality = edge.Cardinality,
            IsRequired = edge.IsRequired
        };
    }

    private static HashSet<string> GetKnownEntityKeys(IReadOnlyList<RelationshipEdge> graph)
    {
        var entityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in graph)
        {
            entityKeys.Add(edge.ParentEntityKey);
            entityKeys.Add(edge.ChildEntityKey);
        }

        return entityKeys;
    }

    private static string NormalizeEntityKey(string entityKey, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(entityKey))
        {
            throw new ArgumentException("Entity key is required.", parameterName);
        }

        return entityKey.Trim().ToLowerInvariant();
    }

    private sealed record SearchState(
        JoinPathNode Node,
        int Depth,
        HashSet<string> VisitedEntityKeys,
        HashSet<string> JoinSignatures);
}
