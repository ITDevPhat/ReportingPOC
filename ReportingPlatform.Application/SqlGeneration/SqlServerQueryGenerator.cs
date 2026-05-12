using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Metadata;
using ReportingPlatform.Domain.QueryPlanning;
using ReportingPlatform.Domain.SqlGeneration;

namespace ReportingPlatform.Application.SqlGeneration;

public sealed class SqlServerQueryGenerator : ISqlGenerator
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 10000;

    private readonly ILogger<SqlServerQueryGenerator> _logger;

    public SqlServerQueryGenerator(ILogger<SqlServerQueryGenerator> logger)
    {
        _logger = logger;
    }

    public Task<GeneratedSqlResult> GenerateAsync(
        QueryPlan queryPlan,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (queryPlan.SelectFields.Count == 0 && queryPlan.Metrics.Count == 0)
        {
            throw new InvalidOperationException("QueryPlan must contain at least one select field or metric.");
        }

        var warnings = new List<string>();
        var parameters = new Dictionary<string, object?>();
        var limit = NormalizeLimit(queryPlan.Limit, warnings);
        var sql = new StringBuilder();

        sql.AppendLine($"SELECT TOP ({limit})");
        sql.AppendLine(string.Join(",\n", BuildSelectItems(queryPlan).Select(item => $"    {item}")));
        sql.AppendLine($"FROM {BuildTableReference(queryPlan.BaseEntity.EntityName)} AS {QuoteIdentifier(queryPlan.BaseEntity.Alias)}");

        foreach (var join in BuildJoinClauses(queryPlan))
        {
            sql.AppendLine(join);
        }

        var whereClauses = BuildWhereClauses(queryPlan, parameters);
        if (whereClauses.Count > 0)
        {
            sql.AppendLine("WHERE " + string.Join("\n    AND ", whereClauses));
        }

        var groupByItems = BuildGroupByItems(queryPlan);
        if (groupByItems.Count > 0)
        {
            sql.AppendLine("GROUP BY");
            sql.AppendLine(string.Join(",\n", groupByItems.Select(item => $"    {item}")));
        }

        var orderByItems = BuildOrderByItems(queryPlan);
        if (orderByItems.Count > 0)
        {
            sql.AppendLine("ORDER BY " + string.Join(", ", orderByItems));
        }

        var generatedSql = sql.ToString().TrimEnd() + ";";
        _logger.LogInformation("Generated SQL with {ParameterCount} parameters.", parameters.Count);

        return Task.FromResult(new GeneratedSqlResult
        {
            Sql = generatedSql,
            Parameters = parameters,
            Warnings = warnings
        });
    }

    private static IReadOnlyList<string> BuildSelectItems(QueryPlan queryPlan)
    {
        var selectItems = new List<string>();

        selectItems.AddRange(queryPlan.SelectFields.Select(field =>
            $"{BuildColumnReference(field.EntityAlias, field.PhysicalColumnName)} AS {QuoteIdentifier(field.OutputAlias)}"));
        selectItems.AddRange(queryPlan.Metrics.Select(metric =>
            $"{BuildMetricExpression(metric)} AS {QuoteIdentifier(metric.Alias)}"));

        return selectItems;
    }

    private static string BuildMetricExpression(ResolvedMetricPlan metric)
    {
        var columnReference = BuildColumnReference(metric.EntityAlias, metric.PhysicalColumnName);

        return metric.Function switch
        {
            "Count" => $"COUNT({columnReference})",
            "CountDistinct" => $"COUNT(DISTINCT {columnReference})",
            "Sum" => $"SUM({columnReference})",
            "Avg" => $"AVG({columnReference})",
            "Min" => $"MIN({columnReference})",
            "Max" => $"MAX({columnReference})",
            _ => throw new InvalidOperationException($"Unsupported metric function '{metric.Function}'.")
        };
    }

    private static IReadOnlyList<string> BuildJoinClauses(QueryPlan queryPlan)
    {
        var joinClauses = new List<string>();
        var joinedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            queryPlan.BaseEntity.Alias
        };

        foreach (var join in queryPlan.Joins)
        {
            var joinParent = joinedAliases.Contains(join.ChildAlias) && !joinedAliases.Contains(join.ParentAlias);
            var joinedAlias = joinParent ? join.ParentAlias : join.ChildAlias;
            var schemaName = joinParent ? join.ParentPhysicalSchemaName : join.ChildPhysicalSchemaName;
            var tableName = joinParent ? join.ParentPhysicalTableName : join.ChildPhysicalTableName;
            var joinKeyword = NormalizeJoinType(join.JoinType);

            joinClauses.Add($"{joinKeyword} {BuildTableReference(schemaName, tableName)} AS {QuoteIdentifier(joinedAlias)}");
            joinClauses.Add($"    ON {BuildColumnReference(join.ParentAlias, join.ParentPhysicalColumnName)} = {BuildColumnReference(join.ChildAlias, join.ChildPhysicalColumnName)}");
            joinedAliases.Add(joinedAlias);
        }

        return joinClauses;
    }

    private static IReadOnlyList<string> BuildWhereClauses(
        QueryPlan queryPlan,
        Dictionary<string, object?> parameters)
    {
        return queryPlan.Filters
            .Select(filter => BuildWhereClause(filter, parameters))
            .ToList();
    }

    private static string BuildWhereClause(
        ResolvedFilterPlan filter,
        Dictionary<string, object?> parameters)
    {
        var columnReference = BuildColumnReference(filter.EntityAlias, filter.PhysicalColumnName);

        return filter.Operator switch
        {
            "Equals" => $"{columnReference} = {AddParameter(parameters, filter.Value)}",
            "NotEquals" => $"{columnReference} <> {AddParameter(parameters, filter.Value)}",
            "GreaterThan" => $"{columnReference} > {AddParameter(parameters, filter.Value)}",
            "GreaterThanOrEqual" => $"{columnReference} >= {AddParameter(parameters, filter.Value)}",
            "LessThan" => $"{columnReference} < {AddParameter(parameters, filter.Value)}",
            "LessThanOrEqual" => $"{columnReference} <= {AddParameter(parameters, filter.Value)}",
            "Between" => BuildBetweenClause(columnReference, filter.Value, parameters),
            "In" => BuildInClause(columnReference, filter.Value, parameters),
            "Contains" => $"{columnReference} LIKE '%' + {AddParameter(parameters, filter.Value)} + '%'",
            "StartsWith" => $"{columnReference} LIKE {AddParameter(parameters, filter.Value)} + '%'",
            "EndsWith" => $"{columnReference} LIKE '%' + {AddParameter(parameters, filter.Value)}",
            "IsNull" => $"{columnReference} IS NULL",
            "IsNotNull" => $"{columnReference} IS NOT NULL",
            _ => throw new InvalidOperationException($"Unsupported filter operator '{filter.Operator}'.")
        };
    }

    private static string BuildBetweenClause(
        string columnReference,
        object? value,
        Dictionary<string, object?> parameters)
    {
        var values = GetArrayValues(value);
        if (values.Count != 2)
        {
            throw new InvalidOperationException("Between filter requires exactly 2 values.");
        }

        return $"{columnReference} BETWEEN {AddParameter(parameters, values[0])} AND {AddParameter(parameters, values[1])}";
    }

    private static string BuildInClause(
        string columnReference,
        object? value,
        Dictionary<string, object?> parameters)
    {
        var values = GetArrayValues(value);
        if (values.Count == 0)
        {
            throw new InvalidOperationException("In filter requires at least one value.");
        }

        var parameterNames = values.Select(item => AddParameter(parameters, item));

        return $"{columnReference} IN ({string.Join(", ", parameterNames)})";
    }

    private static IReadOnlyList<string> BuildGroupByItems(QueryPlan queryPlan)
    {
        return queryPlan.GroupByFields
            .Select(field => BuildColumnReference(field.EntityAlias, field.PhysicalColumnName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> BuildOrderByItems(QueryPlan queryPlan)
    {
        return queryPlan.Sorts
            .Select(sort => $"{QuoteIdentifier(ResolveSortField(sort, queryPlan))} {sort.Direction.ToUpperInvariant()}")
            .ToList();
    }

    private static string ResolveSortField(
        ResolvedSortPlan sort,
        QueryPlan queryPlan)
    {
        if (sort.IsMetricAlias)
        {
            return sort.Field;
        }

        var selectField = queryPlan.SelectFields.FirstOrDefault(field =>
            string.Equals(field.OutputAlias, sort.Field, StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.SemanticKey, sort.Field, StringComparison.OrdinalIgnoreCase));

        return selectField?.OutputAlias ?? sort.Field;
    }

    private static int NormalizeLimit(int limit, List<string> warnings)
    {
        if (limit <= 0)
        {
            warnings.Add($"Invalid limit {limit}; using default limit {DefaultLimit}.");
            return DefaultLimit;
        }

        if (limit > MaxLimit)
        {
            warnings.Add($"Limit {limit} exceeds maximum {MaxLimit}; using {MaxLimit}.");
            return MaxLimit;
        }

        return limit;
    }

    private static string AddParameter(
        Dictionary<string, object?> parameters,
        object? value)
    {
        var name = $"@p{parameters.Count}";
        parameters[name] = NormalizeParameterValue(value);

        return name;
    }

    private static object? NormalizeParameterValue(object? value)
    {
        if (value is not JsonElement jsonElement)
        {
            return value;
        }

        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number when jsonElement.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when jsonElement.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => jsonElement.ToString()
        };
    }

    private static List<object?> GetArrayValues(object? value)
    {
        if (value is JsonElement { ValueKind: JsonValueKind.Array } jsonElement)
        {
            var values = new List<object?>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                values.Add(NormalizeParameterValue(item));
            }

            return values;
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            return enumerable.Cast<object?>().ToList();
        }

        return [];
    }

    private static string NormalizeJoinType(string joinType)
    {
        return joinType.ToUpperInvariant() switch
        {
            "INNER" => "INNER JOIN",
            "LEFT" => "LEFT JOIN",
            "LEFT JOIN" => "LEFT JOIN",
            "RIGHT" => "RIGHT JOIN",
            "RIGHT JOIN" => "RIGHT JOIN",
            "FULL" => "FULL JOIN",
            "FULL JOIN" => "FULL JOIN",
            _ => "INNER JOIN"
        };
    }

    private static string BuildTableReference(string entityName)
    {
        var parts = entityName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Entity name '{entityName}' must use schema.table format.");
        }

        return BuildTableReference(parts[0], parts[1]);
    }

    private static string BuildTableReference(string schemaName, string tableName)
    {
        return $"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}";
    }

    private static string BuildColumnReference(string alias, string columnName)
    {
        return $"{QuoteIdentifier(alias)}.{QuoteIdentifier(columnName)}";
    }

    private static string QuoteIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("SQL identifier cannot be empty.");
        }

        return $"[{value.Replace("]", "]]")}]";
    }
}
