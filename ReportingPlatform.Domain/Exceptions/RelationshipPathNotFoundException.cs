namespace ReportingPlatform.Domain.Exceptions;

public sealed class RelationshipPathNotFoundException : Exception
{
    public RelationshipPathNotFoundException(string baseEntityKey, string targetEntityKey)
        : base($"Relationship path from '{baseEntityKey}' to '{targetEntityKey}' was not found.")
    {
        BaseEntityKey = baseEntityKey;
        TargetEntityKey = targetEntityKey;
    }

    public string BaseEntityKey { get; }

    public string TargetEntityKey { get; }
}
