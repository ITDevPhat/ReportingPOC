using ReportingPlatform.Application.DTOs;

namespace ReportingPlatform.Application.Interfaces;

public interface ISemanticMetadataProvider
{
    Task<IReadOnlyList<ReportingEntityDto>> GetEntitiesAsync(CancellationToken cancellationToken = default);

    Task<ReportingEntityDto?> GetEntityAsync(string entityKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportingFieldDto>> GetFieldsByEntityKeyAsync(string entityKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportingRelationshipDto>> GetRelationshipsAsync(CancellationToken cancellationToken = default);
}
