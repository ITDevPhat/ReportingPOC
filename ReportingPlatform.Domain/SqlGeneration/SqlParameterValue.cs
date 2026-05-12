namespace ReportingPlatform.Domain.SqlGeneration;

public sealed class SqlParameterValue
{
    public string Name { get; set; } = string.Empty;
    public object? Value { get; set; }
}
