namespace ReportingPlatform.Domain.Exceptions;

public sealed class AmbiguousRelationshipPathException : Exception
{
    public AmbiguousRelationshipPathException(string baseEntityKey, string targetEntityKey)
        : base($"Multiple shortest relationship paths from '{baseEntityKey}' to '{targetEntityKey}' were found.")
    {
        BaseEntityKey = baseEntityKey;
        TargetEntityKey = targetEntityKey;
    }

    public string BaseEntityKey { get; }

    public string TargetEntityKey { get; }
}
