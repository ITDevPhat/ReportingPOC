namespace ReportingPlatform.Domain.Exceptions;

public sealed class InvalidSemanticKeyException : Exception
{
    public InvalidSemanticKeyException(string message)
        : base(message)
    {
    }
}
