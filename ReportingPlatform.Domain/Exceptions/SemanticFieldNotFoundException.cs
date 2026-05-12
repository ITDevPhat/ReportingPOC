namespace ReportingPlatform.Domain.Exceptions;

public sealed class SemanticFieldNotFoundException : Exception
{
    public SemanticFieldNotFoundException(string semanticKey)
        : base($"Semantic field '{semanticKey}' was not found.")
    {
        SemanticKey = semanticKey;
    }

    public string SemanticKey { get; }
}
