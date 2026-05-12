using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.Results;
using ReportingPlatform.Infrastructure.Persistence;

namespace ReportingPlatform.Infrastructure.Execution;

public sealed class SqlServerQueryExecutor : IQueryExecutor
{
    private const int DefaultCommandTimeoutSeconds = 60;

    private readonly IQueryPlanBuilder _queryPlanBuilder;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlServerQueryExecutor> _logger;
    private readonly int _commandTimeoutSeconds;

    public SqlServerQueryExecutor(
        IQueryPlanBuilder queryPlanBuilder,
        ISqlGenerator sqlGenerator,
        ISqlConnectionFactory connectionFactory,
        IConfiguration configuration,
        ILogger<SqlServerQueryExecutor> logger)
    {
        _queryPlanBuilder = queryPlanBuilder;
        _sqlGenerator = sqlGenerator;
        _connectionFactory = connectionFactory;
        _logger = logger;
        _commandTimeoutSeconds = int.TryParse(
            configuration["ReportingEngine:CommandTimeoutSeconds"],
            out var configuredTimeoutSeconds)
            ? configuredTimeoutSeconds
            : DefaultCommandTimeoutSeconds;
    }

    public async Task<QueryExecutionResult> ExecuteAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var queryPlan = await _queryPlanBuilder.BuildAsync(request, cancellationToken);
            var generatedSql = await _sqlGenerator.GenerateAsync(queryPlan, cancellationToken);

            await using var connection = GetSqlConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = generatedSql.Sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = _commandTimeoutSeconds;

            foreach (var parameter in generatedSql.Parameters)
            {
                command.Parameters.Add(new SqlParameter(parameter.Key, parameter.Value ?? DBNull.Value));
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var columns = ReadColumns(reader);
            var rows = await ReadRowsAsync(reader, columns, cancellationToken);

            stopwatch.Stop();

            return new QueryExecutionResult
            {
                Success = true,
                Columns = columns,
                Rows = rows,
                GeneratedSql = generatedSql.Sql,
                Parameters = generatedSql.Parameters,
                RowCount = rows.Count,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Warnings = generatedSql.Warnings
            };
        }
        catch (InvalidReportQueryException)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(exception, "Failed to execute report query.");

            return new QueryExecutionResult
            {
                Success = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Error = "Failed to execute report query."
            };
        }
    }

    private SqlConnection GetSqlConnection()
    {
        var connection = _connectionFactory.CreateConnection();
        if (connection is SqlConnection sqlConnection)
        {
            return sqlConnection;
        }

        connection.Dispose();
        throw new InvalidOperationException("SQL connection factory must create a Microsoft.Data.SqlClient.SqlConnection.");
    }

    private static List<QueryColumnResult> ReadColumns(SqlDataReader reader)
    {
        var columns = new List<QueryColumnResult>(reader.FieldCount);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(new QueryColumnResult
            {
                Name = reader.GetName(i),
                DataType = MapDataType(reader.GetFieldType(i))
            });
        }

        return columns;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(
        SqlDataReader reader,
        IReadOnlyList<QueryColumnResult> columns,
        CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < columns.Count; i++)
            {
                var value = await reader.IsDBNullAsync(i, cancellationToken)
                    ? null
                    : reader.GetValue(i);
                row[columns[i].Name] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string MapDataType(Type type)
    {
        if (type == typeof(string))
        {
            return "String";
        }

        if (type == typeof(int))
        {
            return "Int32";
        }

        if (type == typeof(decimal))
        {
            return "Decimal";
        }

        if (type == typeof(DateTime))
        {
            return "DateTime";
        }

        if (type == typeof(bool))
        {
            return "Boolean";
        }

        if (type == typeof(double))
        {
            return "Double";
        }

        return type.Name;
    }
}
