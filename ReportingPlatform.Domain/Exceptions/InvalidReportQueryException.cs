namespace ReportingPlatform.Domain.Exceptions;

public sealed class InvalidReportQueryException : Exception
{
    public InvalidReportQueryException(IEnumerable<string> errors)
        : base("Report query request is invalid.")
    {
        Errors = errors.ToList();
    }

    public List<string> Errors { get; }
}
