namespace ReportingPlatform.Domain.SqlGeneration;

public sealed class GeneratedSqlResult
{
    public string Sql { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
