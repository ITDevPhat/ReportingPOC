using ReportingPlatform.Domain.Metadata;

namespace ReportingPlatform.Application.Interfaces;

public interface IRelationshipResolver
{
    Task<RelationshipPath> ResolvePathAsync(
        string baseEntityKey,
        string targetEntityKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RelationshipPath>> ResolvePathsAsync(
        string baseEntityKey,
        IEnumerable<string> targetEntityKeys,
        CancellationToken cancellationToken = default);
}
