using ReportingPlatform.Domain.Exceptions;

namespace ReportingPlatform.Domain.Metadata;

public sealed class SemanticFieldKey
{
    private SemanticFieldKey(string entityKey, string fieldKey, string rawKey)
    {
        EntityKey = entityKey;
        FieldKey = fieldKey;
        RawKey = rawKey;
    }

    public string EntityKey { get; }

    public string FieldKey { get; }

    public string RawKey { get; }

    public static SemanticFieldKey Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidSemanticKeyException("Semantic key is required.");
        }

        var rawKey = value.Trim().ToLowerInvariant();
        var parts = rawKey.Split('.');

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new InvalidSemanticKeyException(
                $"Invalid semantic key '{value}'. Expected format is '{{entityKey}}.{{fieldKey}}'.");
        }

        var entityKey = parts[0].Trim();
        var fieldKey = parts[1].Trim();

        if (entityKey.Length == 0 || fieldKey.Length == 0)
        {
            throw new InvalidSemanticKeyException(
                $"Invalid semantic key '{value}'. Entity key and field key are required.");
        }

        return new SemanticFieldKey(entityKey, fieldKey, $"{entityKey}.{fieldKey}");
    }
}
