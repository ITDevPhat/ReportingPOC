namespace ReportingPlatform.Domain.Exceptions;

public sealed class SemanticEntityNotFoundException : Exception
{
    public SemanticEntityNotFoundException(string entityKey)
        : base($"Semantic entity '{entityKey}' was not found.")
    {
        EntityKey = entityKey;
    }

    public string EntityKey { get; }
}
