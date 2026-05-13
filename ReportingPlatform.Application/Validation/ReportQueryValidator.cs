using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Enums;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Metadata;
using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.Validation;

public sealed class ReportQueryValidator : IReportQueryValidator
{
    private readonly ISemanticResolver _semanticResolver;
    private readonly IRelationshipResolver _relationshipResolver;
    private readonly ILogger<ReportQueryValidator> _logger;
    private readonly int _maxRows;
    private readonly int _maxSelectedFields;
    private readonly int _maxMetrics;
    private readonly int _maxFilters;
    private readonly int _maxGroupByFields;

    public ReportQueryValidator(
        ISemanticResolver semanticResolver,
        IRelationshipResolver relationshipResolver,
        IConfiguration configuration,
        ILogger<ReportQueryValidator> logger)
    {
        _semanticResolver = semanticResolver;
        _relationshipResolver = relationshipResolver;
        _logger = logger;
        _maxRows = GetInt(configuration, "ReportingEngine:MaxRows", 10000);
        _maxSelectedFields = GetInt(configuration, "ReportingEngine:MaxSelectedFields", 50);
        _maxMetrics = GetInt(configuration, "ReportingEngine:MaxMetrics", 20);
        _maxFilters = GetInt(configuration, "ReportingEngine:MaxFilters", 30);
        _maxGroupByFields = GetInt(configuration, "ReportingEngine:MaxGroupByFields", 20);
    }

    public async Task ValidateAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeForValidation(request);
        var errors = new List<string>();
        var resolvedFields = new Dictionary<string, ResolvedField>(StringComparer.OrdinalIgnoreCase);
        var requiredEntityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Report query validation started. SelectFields={SelectFieldCount} Metrics={MetricCount} Filters={FilterCount} GroupBy={GroupByCount}",
            normalizedRequest.SelectFields.Count,
            normalizedRequest.Metrics.Count,
            normalizedRequest.Filters.Count,
            normalizedRequest.GroupBy.Count);

        ValidateBaseEntity(normalizedRequest, errors);
        ValidateLimit(normalizedRequest, errors);
        ValidateCollectionLimits(normalizedRequest, errors);
        await ValidateSelectFieldsAsync(normalizedRequest, resolvedFields, requiredEntityKeys, errors, cancellationToken);
        await ValidateGroupByAsync(normalizedRequest, resolvedFields, requiredEntityKeys, errors, cancellationToken);
        await ValidateMetricsAsync(normalizedRequest, resolvedFields, requiredEntityKeys, errors, cancellationToken);
        await ValidateFiltersAsync(normalizedRequest, resolvedFields, requiredEntityKeys, errors, cancellationToken);
        ValidateSort(normalizedRequest, errors);
        await ValidateRelationshipsAsync(normalizedRequest, requiredEntityKeys, errors, cancellationToken);

        if (errors.Count > 0)
        {
            _logger.LogWarning("Report query request failed validation with {ErrorCount} errors.", errors.Count);
            throw new InvalidReportQueryException(errors);
        }

        _logger.LogInformation("Report query validation succeeded.");
    }

    public ReportQueryRequest Normalize(ReportQueryRequest request)
    {
        var baseEntity = NormalizeKey(request.BaseEntity);

        return new ReportQueryRequest
        {
            BaseEntity = baseEntity,
            SelectFields = NormalizeDistinctKeys(request.SelectFields),
            Metrics = request.Metrics
                .Where(metric => metric is not null)
                .Select(metric => new QueryMetricDefinition
                {
                    MetricKey = NormalizeKey(metric.MetricKey),
                    Function = metric.Function,
                    Field = NormalizeKey(metric.Field),
                    Alias = metric.Alias.Trim()
                })
                .ToList(),
            Filters = request.Filters
                .Where(filter => filter is not null)
                .Select(filter => new QueryFilterDefinition
                {
                    Field = NormalizeKey(filter.Field),
                    Operator = filter.Operator,
                    Value = filter.Value
                })
                .ToList(),
            GroupBy = NormalizeDistinctKeys(request.GroupBy),
            Sort = request.Sort
                .Where(sort => sort is not null)
                .Select(sort => new QuerySortDefinition
                {
                    Field = sort.Field.Trim(),
                    Direction = sort.Direction
                })
                .ToList(),
            Limit = request.Limit
        };
    }

    private static void ValidateBaseEntity(ReportQueryRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.BaseEntity))
        {
            errors.Add("Base entity is required.");
        }
    }

    private void ValidateLimit(ReportQueryRequest request, List<string> errors)
    {
        if (request.Limit <= 0)
        {
            errors.Add("Limit must be greater than 0.");
        }
        else if (request.Limit > _maxRows)
        {
            errors.Add($"Limit must be less than or equal to {_maxRows}.");
        }
    }

    private void ValidateCollectionLimits(ReportQueryRequest request, List<string> errors)
    {
        if (request.SelectFields.Count > _maxSelectedFields)
        {
            errors.Add($"Select fields count must be less than or equal to {_maxSelectedFields}.");
        }

        if (request.Metrics.Count > _maxMetrics)
        {
            errors.Add($"Metrics count must be less than or equal to {_maxMetrics}.");
        }

        if (request.Filters.Count > _maxFilters)
        {
            errors.Add($"Filters count must be less than or equal to {_maxFilters}.");
        }

        if (request.GroupBy.Count > _maxGroupByFields)
        {
            errors.Add($"Group by fields count must be less than or equal to {_maxGroupByFields}.");
        }
    }

    private async Task ValidateSelectFieldsAsync(
        ReportQueryRequest request,
        Dictionary<string, ResolvedField> resolvedFields,
        HashSet<string> requiredEntityKeys,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        AddDuplicateErrors(request.SelectFields, "Select field", errors);

        foreach (var field in request.SelectFields)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                errors.Add("Select field cannot be empty.");
                continue;
            }

            var resolvedField = await TryResolveFieldAsync(field, errors, cancellationToken);
            AddResolvedField(resolvedField, resolvedFields, requiredEntityKeys);
        }
    }

    private async Task ValidateGroupByAsync(
        ReportQueryRequest request,
        Dictionary<string, ResolvedField> resolvedFields,
        HashSet<string> requiredEntityKeys,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        AddDuplicateErrors(request.GroupBy, "Group by field", errors);

        foreach (var field in request.GroupBy)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                errors.Add("Group by field cannot be empty.");
                continue;
            }

            var resolvedField = await TryResolveFieldAsync(field, errors, cancellationToken);
            AddResolvedField(resolvedField, resolvedFields, requiredEntityKeys);

            if (resolvedField is not null && !resolvedField.IsGroupable)
            {
                errors.Add($"Field {resolvedField.SemanticKey} is not groupable.");
            }
        }
    }

    private async Task ValidateMetricsAsync(
        ReportQueryRequest request,
        Dictionary<string, ResolvedField> resolvedFields,
        HashSet<string> requiredEntityKeys,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var metric in request.Metrics)
        {
            if (string.IsNullOrWhiteSpace(metric.MetricKey))
            {
                errors.Add("Metric key is required.");
            }

            if (string.IsNullOrWhiteSpace(metric.Alias))
            {
                errors.Add("Metric alias is required.");
            }
            else if (!aliases.Add(metric.Alias))
            {
                errors.Add($"Duplicate metric alias '{metric.Alias}' is not allowed.");
            }

            if (string.IsNullOrWhiteSpace(metric.Field))
            {
                errors.Add($"Metric '{metric.MetricKey}' field is required.");
                continue;
            }

            var resolvedField = await TryResolveFieldAsync(metric.Field, errors, cancellationToken);
            AddResolvedField(resolvedField, resolvedFields, requiredEntityKeys);
            ValidateMetricCompatibility(metric, resolvedField, errors);
        }
    }

    private async Task ValidateFiltersAsync(
        ReportQueryRequest request,
        Dictionary<string, ResolvedField> resolvedFields,
        HashSet<string> requiredEntityKeys,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var filter in request.Filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Field))
            {
                errors.Add("Filter field is required.");
                continue;
            }

            var resolvedField = await TryResolveFieldAsync(filter.Field, errors, cancellationToken);
            AddResolvedField(resolvedField, resolvedFields, requiredEntityKeys);

            if (resolvedField is null)
            {
                continue;
            }

            if (!resolvedField.IsFilterable)
            {
                errors.Add($"Field {resolvedField.SemanticKey} is not filterable.");
            }

            ValidateFilterOperator(filter, resolvedField, errors);
        }
    }

    private static void ValidateSort(ReportQueryRequest request, List<string> errors)
    {
        var selectedFields = new HashSet<string>(request.SelectFields, StringComparer.OrdinalIgnoreCase);
        var metricAliases = request.Metrics
            .Select(metric => metric.Alias)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var sort in request.Sort)
        {
            if (string.IsNullOrWhiteSpace(sort.Field))
            {
                errors.Add("Sort field is required.");
                continue;
            }

            if (!selectedFields.Contains(sort.Field) && !metricAliases.Contains(sort.Field))
            {
                errors.Add($"Sort field '{sort.Field}' must be selected or match a metric alias.");
            }
        }
    }

    private async Task ValidateRelationshipsAsync(
        ReportQueryRequest request,
        HashSet<string> requiredEntityKeys,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BaseEntity))
        {
            return;
        }

        foreach (var entityKey in requiredEntityKeys.Where(entityKey => !string.Equals(entityKey, request.BaseEntity, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                await _relationshipResolver.ResolvePathAsync(request.BaseEntity, entityKey, cancellationToken);
            }
            catch (Exception exception) when (exception is RelationshipPathNotFoundException or AmbiguousRelationshipPathException)
            {
                errors.Add(exception.Message);
            }
        }
    }

    private async Task<ResolvedField?> TryResolveFieldAsync(
        string field,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _semanticResolver.ResolveFieldAsync(field, cancellationToken);
        }
        catch (InvalidSemanticKeyException exception)
        {
            errors.Add(exception.Message);
        }
        catch (SemanticEntityNotFoundException exception)
        {
            errors.Add(exception.Message);
        }
        catch (SemanticFieldNotFoundException exception)
        {
            errors.Add($"Field {exception.SemanticKey} does not exist.");
        }

        return null;
    }

    private static void AddResolvedField(
        ResolvedField? resolvedField,
        Dictionary<string, ResolvedField> resolvedFields,
        HashSet<string> requiredEntityKeys)
    {
        if (resolvedField is null)
        {
            return;
        }

        resolvedFields.TryAdd(resolvedField.SemanticKey, resolvedField);
        requiredEntityKeys.Add(resolvedField.EntityKey);
    }

    private static void ValidateMetricCompatibility(
        QueryMetricDefinition metric,
        ResolvedField? resolvedField,
        List<string> errors)
    {
        if (resolvedField is null)
        {
            return;
        }

        if (metric.Function is MetricFunction.Sum or MetricFunction.Avg && !IsNumericType(resolvedField.DataType))
        {
            errors.Add($"Metric '{metric.MetricKey}' requires a numeric field for {metric.Function}.");
        }

        if (metric.Function is MetricFunction.CountDistinct && !resolvedField.IsIdentifier)
        {
            errors.Add($"Metric '{metric.MetricKey}' should use an identifier field for CountDistinct.");
        }
    }

    private static void ValidateFilterOperator(
        QueryFilterDefinition filter,
        ResolvedField resolvedField,
        List<string> errors)
    {
        switch (filter.Operator)
        {
            case FilterOperator.Between:
                if (GetArrayLength(filter.Value) != 2)
                {
                    errors.Add($"Filter {resolvedField.SemanticKey} with Between requires exactly 2 values.");
                }
                break;
            case FilterOperator.In:
                if (!IsArray(filter.Value))
                {
                    errors.Add($"Filter {resolvedField.SemanticKey} with In requires an array value.");
                }
                break;
            case FilterOperator.IsNull:
            case FilterOperator.IsNotNull:
                if (!IsNullValue(filter.Value))
                {
                    errors.Add($"Filter {resolvedField.SemanticKey} with {filter.Operator} requires a null value.");
                }
                break;
            case FilterOperator.Contains:
            case FilterOperator.StartsWith:
            case FilterOperator.EndsWith:
                if (!IsStringType(resolvedField.DataType))
                {
                    errors.Add($"Filter {resolvedField.SemanticKey} with {filter.Operator} requires a string field.");
                }
                break;
        }

        if (filter.Operator is FilterOperator.GreaterThan
            or FilterOperator.GreaterThanOrEqual
            or FilterOperator.LessThan
            or FilterOperator.LessThanOrEqual
            or FilterOperator.Between)
        {
            if (!IsNumericType(resolvedField.DataType) && !IsDateType(resolvedField.DataType))
            {
                errors.Add($"Filter {resolvedField.SemanticKey} with {filter.Operator} requires a numeric or date field.");
            }
        }
    }

    private static void AddDuplicateErrors(IEnumerable<string> values, string label, List<string> errors)
    {
        var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !seenValues.Add(value))
            {
                errors.Add($"Duplicate {label.ToLowerInvariant()} '{value}' is not allowed.");
            }
        }
    }

    private static List<string> NormalizeDistinctKeys(IEnumerable<string> values)
    {
        var keys = new List<string>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var key = NormalizeKey(value);
            if (key.Length > 0 && seenKeys.Add(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private static ReportQueryRequest NormalizeForValidation(ReportQueryRequest request)
    {
        return new ReportQueryRequest
        {
            BaseEntity = NormalizeKey(request.BaseEntity),
            SelectFields = request.SelectFields.Select(NormalizeKey).ToList(),
            Metrics = request.Metrics
                .Where(metric => metric is not null)
                .Select(metric => new QueryMetricDefinition
                {
                    MetricKey = NormalizeKey(metric.MetricKey),
                    Function = metric.Function,
                    Field = NormalizeKey(metric.Field),
                    Alias = metric.Alias.Trim()
                })
                .ToList(),
            Filters = request.Filters
                .Where(filter => filter is not null)
                .Select(filter => new QueryFilterDefinition
                {
                    Field = NormalizeKey(filter.Field),
                    Operator = filter.Operator,
                    Value = filter.Value
                })
                .ToList(),
            GroupBy = request.GroupBy.Select(NormalizeKey).ToList(),
            Sort = request.Sort
                .Where(sort => sort is not null)
                .Select(sort => new QuerySortDefinition
                {
                    Field = sort.Field.Trim(),
                    Direction = sort.Direction
                })
                .ToList(),
            Limit = request.Limit
        };
    }

    private static string NormalizeKey(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static bool IsNumericType(string dataType)
    {
        return dataType.Equals("int", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("bigint", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("smallint", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("tinyint", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("decimal", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("numeric", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("money", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("smallmoney", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("float", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("real", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStringType(string dataType)
    {
        return dataType.Equals("char", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("varchar", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("nchar", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("text", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("ntext", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDateType(string dataType)
    {
        return dataType.Equals("date", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("datetime", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("datetime2", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("smalldatetime", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("datetimeoffset", StringComparison.OrdinalIgnoreCase)
            || dataType.Equals("time", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNullValue(object? value)
    {
        return value is null || value is JsonElement { ValueKind: JsonValueKind.Null };
    }

    private static bool IsArray(object? value)
    {
        return value is JsonElement { ValueKind: JsonValueKind.Array }
            || value is System.Collections.IEnumerable && value is not string;
    }

    private static int? GetArrayLength(object? value)
    {
        if (value is JsonElement { ValueKind: JsonValueKind.Array } jsonElement)
        {
            return jsonElement.GetArrayLength();
        }

        if (value is System.Collections.ICollection collection && value is not string)
        {
            return collection.Count;
        }

        return null;
    }

    private static int GetInt(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value) ? value : fallback;
    }
}
