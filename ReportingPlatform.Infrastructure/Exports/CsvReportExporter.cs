using System.Text;
using ReportingPlatform.Domain.Results;

namespace ReportingPlatform.Infrastructure.Exports;

public sealed class CsvReportExporter
{
    public Task<Stream> ExportAsync(QueryExecutionResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", result.Columns.Select(column => Escape(column.Name))));

        foreach (var row in result.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = result.Columns.Select(column =>
                row.TryGetValue(column.Name, out var value) ? Escape(FormatValue(value)) : string.Empty);
            builder.AppendLine(string.Join(",", values));
        }

        return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',', StringComparison.Ordinal)
            && !value.Contains('"', StringComparison.Ordinal)
            && !value.Contains('\r', StringComparison.Ordinal)
            && !value.Contains('\n', StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
