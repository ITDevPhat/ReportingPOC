namespace ReportingPlatform.Domain.Results;

public sealed class QueryExecutionResult
{
    public bool Success { get; set; }
    public List<QueryColumnResult> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public string GeneratedSql { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = [];
    public int RowCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? Error { get; set; }
    public List<string> Warnings { get; set; } = [];
}
