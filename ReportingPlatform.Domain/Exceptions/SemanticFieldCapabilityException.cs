namespace ReportingPlatform.Domain.Exceptions;

public sealed class SemanticFieldCapabilityException : Exception
{
    public SemanticFieldCapabilityException(string semanticKey, string capability)
        : base($"Semantic field '{semanticKey}' is not {capability}.")
    {
        SemanticKey = semanticKey;
        Capability = capability;
    }

    public string SemanticKey { get; }

    public string Capability { get; }
}
