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
    private readonly IQueryHashService _queryHashService;
    private readonly IQueryResultCache _queryResultCache;
    private readonly ILogger<SqlServerQueryExecutor> _logger;
    private readonly int _commandTimeoutSeconds;
    private readonly bool _enableQueryCaching;
    private readonly TimeSpan _queryCacheTtl;

    public SqlServerQueryExecutor(
        IQueryPlanBuilder queryPlanBuilder,
        ISqlGenerator sqlGenerator,
        ISqlConnectionFactory connectionFactory,
        IQueryHashService queryHashService,
        IQueryResultCache queryResultCache,
        IConfiguration configuration,
        ILogger<SqlServerQueryExecutor> logger)
    {
        _queryPlanBuilder = queryPlanBuilder;
        _sqlGenerator = sqlGenerator;
        _connectionFactory = connectionFactory;
        _queryHashService = queryHashService;
        _queryResultCache = queryResultCache;
        _logger = logger;
        _commandTimeoutSeconds = int.TryParse(
            configuration["ReportingEngine:CommandTimeoutSeconds"],
            out var configuredTimeoutSeconds)
            ? configuredTimeoutSeconds
            : DefaultCommandTimeoutSeconds;
        _enableQueryCaching = bool.TryParse(configuration["ReportingEngine:EnableQueryCaching"], out var enableCaching)
            && enableCaching;
        var cacheTtlMinutes = int.TryParse(configuration["ReportingEngine:QueryCacheTtlMinutes"], out var configuredCacheTtlMinutes)
            ? configuredCacheTtlMinutes
            : 30;
        _queryCacheTtl = TimeSpan.FromMinutes(cacheTtlMinutes);
    }

    public async Task<QueryExecutionResult> ExecuteAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var queryHash = _queryHashService.ComputeHash(request);

        try
        {
            if (_enableQueryCaching)
            {
                var cachedResult = await _queryResultCache.GetAsync(queryHash, cancellationToken);
                if (cachedResult is not null)
                {
                    stopwatch.Stop();
                    if (!cachedResult.Warnings.Contains("Returned from cache", StringComparer.OrdinalIgnoreCase))
                    {
                        cachedResult.Warnings.Add("Returned from cache");
                    }

                    _logger.LogInformation(
                        "Returned report query result from cache. QueryHash={QueryHash} RowCount={RowCount}",
                        queryHash,
                        cachedResult.RowCount);

                    return cachedResult;
                }
            }

            var queryPlan = await _queryPlanBuilder.BuildAsync(request, cancellationToken);
            var generatedSql = await _sqlGenerator.GenerateAsync(queryPlan, cancellationToken);

            _logger.LogInformation("Executing report query. QueryHash={QueryHash}", queryHash);

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

            var result = new QueryExecutionResult
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

            if (_enableQueryCaching)
            {
                await _queryResultCache.SetAsync(queryHash, result, _queryCacheTtl, cancellationToken);
            }

            _logger.LogInformation(
                "Executed report query. QueryHash={QueryHash} RowCount={RowCount} DurationMs={DurationMs}",
                queryHash,
                result.RowCount,
                result.ExecutionTimeMs);

            return result;
        }
        catch (InvalidReportQueryException)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(exception, "Failed to execute report query. QueryHash={QueryHash}", queryHash);

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
