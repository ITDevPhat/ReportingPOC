using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.DTOs;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Metadata;

namespace ReportingPlatform.Application.Resolvers;

public sealed class SemanticResolver : ISemanticResolver
{
    private readonly ISemanticMetadataProvider _metadataProvider;
    private readonly ILogger<SemanticResolver> _logger;

    public SemanticResolver(
        ISemanticMetadataProvider metadataProvider,
        ILogger<SemanticResolver> logger)
    {
        _metadataProvider = metadataProvider;
        _logger = logger;
    }

    public async Task<ResolvedField> ResolveFieldAsync(string semanticKey, CancellationToken cancellationToken = default)
    {
        var parsedKey = SemanticFieldKey.Parse(semanticKey);

        var entity = await _metadataProvider.GetEntityAsync(parsedKey.EntityKey, cancellationToken);
        if (entity is null)
        {
            _logger.LogWarning("Semantic entity {EntityKey} was not found while resolving {SemanticKey}.", parsedKey.EntityKey, parsedKey.RawKey);
            throw new SemanticEntityNotFoundException(parsedKey.EntityKey);
        }

        var fields = await _metadataProvider.GetFieldsByEntityKeyAsync(parsedKey.EntityKey, cancellationToken);
        var field = fields.FirstOrDefault(x => string.Equals(x.FieldKey, parsedKey.FieldKey, StringComparison.OrdinalIgnoreCase));

        if (field is null)
        {
            _logger.LogWarning("Semantic field {SemanticKey} was not found.", parsedKey.RawKey);
            throw new SemanticFieldNotFoundException(parsedKey.RawKey);
        }

        return MapResolvedField(entity, field);
    }

    public async Task<IReadOnlyList<ResolvedField>> ResolveFieldsAsync(IEnumerable<string> semanticKeys, CancellationToken cancellationToken = default)
    {
        if (semanticKeys is null)
        {
            throw new InvalidSemanticKeyException("At least one semantic key is required.");
        }

        var distinctKeys = new List<string>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var semanticKey in semanticKeys)
        {
            var parsedKey = SemanticFieldKey.Parse(semanticKey);

            if (seenKeys.Add(parsedKey.RawKey))
            {
                distinctKeys.Add(parsedKey.RawKey);
            }
        }

        var resolvedFields = new List<ResolvedField>(distinctKeys.Count);

        foreach (var semanticKey in distinctKeys)
        {
            resolvedFields.Add(await ResolveFieldAsync(semanticKey, cancellationToken));
        }

        return resolvedFields;
    }

    public async Task ValidateFieldIsFilterableAsync(string semanticKey, CancellationToken cancellationToken = default)
    {
        var resolvedField = await ResolveFieldAsync(semanticKey, cancellationToken);

        if (!resolvedField.IsFilterable)
        {
            throw new SemanticFieldCapabilityException(resolvedField.SemanticKey, "filterable");
        }
    }

    public async Task ValidateFieldIsGroupableAsync(string semanticKey, CancellationToken cancellationToken = default)
    {
        var resolvedField = await ResolveFieldAsync(semanticKey, cancellationToken);

        if (!resolvedField.IsGroupable)
        {
            throw new SemanticFieldCapabilityException(resolvedField.SemanticKey, "groupable");
        }
    }

    private static ResolvedField MapResolvedField(ReportingEntityDto entity, ReportingFieldDto field)
    {
        return new ResolvedField
        {
            SemanticKey = field.SemanticKey,
            EntityId = entity.EntityId,
            EntityKey = entity.EntityKey,
            EntityDisplayName = entity.DisplayName,
            FieldId = field.FieldId,
            FieldKey = field.FieldKey,
            FieldDisplayName = field.DisplayName,
            DataType = field.DataType,
            FieldRole = field.FieldRole,
            IsSystemFilter = field.IsSystemFilter,
            IsFilterable = field.IsFilterable,
            IsGroupable = field.IsGroupable,
            IsSensitive = field.IsSensitive,
            IsDefault = field.IsDefault,
            IsIdentifier = field.IsIdentifier,
            PhysicalSchemaName = field.PhysicalSchemaName,
            PhysicalTableName = field.PhysicalTableName,
            PhysicalColumnName = field.PhysicalColumnName,
            ExpressionSql = field.ExpressionSql
        };
    }
}
