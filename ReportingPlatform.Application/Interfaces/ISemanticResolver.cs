using ReportingPlatform.Domain.Metadata;

namespace ReportingPlatform.Application.Interfaces;

public interface ISemanticResolver
{
    Task<ResolvedField> ResolveFieldAsync(string semanticKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResolvedField>> ResolveFieldsAsync(IEnumerable<string> semanticKeys, CancellationToken cancellationToken = default);

    Task ValidateFieldIsFilterableAsync(string semanticKey, CancellationToken cancellationToken = default);

    Task ValidateFieldIsGroupableAsync(string semanticKey, CancellationToken cancellationToken = default);
}
