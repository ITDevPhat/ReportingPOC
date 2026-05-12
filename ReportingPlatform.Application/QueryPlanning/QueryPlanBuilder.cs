using System.Globalization;
using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exceptions;
using ReportingPlatform.Domain.Metadata;
using ReportingPlatform.Domain.Query;
using ReportingPlatform.Domain.QueryPlanning;

namespace ReportingPlatform.Application.QueryPlanning;

public sealed class QueryPlanBuilder : IQueryPlanBuilder
{
    private readonly IReportQueryValidator _queryValidator;
    private readonly ISemanticResolver _semanticResolver;
    private readonly IRelationshipResolver _relationshipResolver;
    private readonly ISemanticMetadataProvider _metadataProvider;
    private readonly ILogger<QueryPlanBuilder> _logger;

    public QueryPlanBuilder(
        IReportQueryValidator queryValidator,
        ISemanticResolver semanticResolver,
        IRelationshipResolver relationshipResolver,
        ISemanticMetadataProvider metadataProvider,
        ILogger<QueryPlanBuilder> logger)
    {
        _queryValidator = queryValidator;
        _semanticResolver = semanticResolver;
        _relationshipResolver = relationshipResolver;
        _metadataProvider = metadataProvider;
        _logger = logger;
    }

    public async Task<QueryPlan> BuildAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await _queryValidator.ValidateAsync(request, cancellationToken);

        var normalizedRequest = _queryValidator.Normalize(request);
        var baseEntity = await _metadataProvider.GetEntityAsync(normalizedRequest.BaseEntity, cancellationToken);
        if (baseEntity is null)
        {
            throw new InvalidReportQueryException(
            [
                $"Base entity '{normalizedRequest.BaseEntity}' does not exist."
            ]);
        }

        var selectFields = await _semanticResolver.ResolveFieldsAsync(normalizedRequest.SelectFields, cancellationToken);
        var metricFields = await ResolveMetricFieldsAsync(normalizedRequest, cancellationToken);
        var filterFields = await ResolveFilterFieldsAsync(normalizedRequest, cancellationToken);
        var groupByFields = await _semanticResolver.ResolveFieldsAsync(normalizedRequest.GroupBy, cancellationToken);

        var requiredEntityKeys = GetRequiredEntityKeys(
            selectFields,
            metricFields,
            filterFields,
            groupByFields);
        var entityAliases = BuildEntityAliases(normalizedRequest.BaseEntity, requiredEntityKeys);

        var selectPlans = selectFields.Select(field => MapSelectField(field, entityAliases)).ToList();
        var metricPlans = normalizedRequest.Metrics
            .Zip(metricFields, (metric, field) => MapMetric(metric, field, entityAliases))
            .ToList();
        var filterPlans = normalizedRequest.Filters
            .Zip(filterFields, (filter, field) => MapFilter(filter, field, entityAliases))
            .ToList();
        var groupByPlans = groupByFields.Select(field => MapGroupBy(field, entityAliases)).ToList();
        var sortPlans = normalizedRequest.Sort
            .Select(sort => MapSort(sort, selectPlans, metricPlans))
            .ToList();
        var joins = await ResolveJoinsAsync(
            normalizedRequest.BaseEntity,
            requiredEntityKeys,
            entityAliases,
            cancellationToken);

        return new QueryPlan
        {
            BaseEntityKey = normalizedRequest.BaseEntity,
            BaseEntity = new ResolvedEntityPlan
            {
                EntityKey = baseEntity.EntityKey,
                EntityName = baseEntity.EntityName,
                DisplayName = baseEntity.DisplayName,
                Alias = GetEntityAlias(baseEntity.EntityKey)
            },
            SelectFields = selectPlans,
            Metrics = metricPlans,
            Filters = filterPlans,
            GroupByFields = groupByPlans,
            Sorts = sortPlans,
            Joins = joins,
            EntityAliases = entityAliases,
            Limit = normalizedRequest.Limit
        };
    }

    private async Task<IReadOnlyList<ResolvedField>> ResolveMetricFieldsAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var fields = new List<ResolvedField>(request.Metrics.Count);

        foreach (var metric in request.Metrics)
        {
            fields.Add(await _semanticResolver.ResolveFieldAsync(metric.Field, cancellationToken));
        }

        return fields;
    }

    private async Task<IReadOnlyList<ResolvedField>> ResolveFilterFieldsAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var fields = new List<ResolvedField>(request.Filters.Count);

        foreach (var filter in request.Filters)
        {
            fields.Add(await _semanticResolver.ResolveFieldAsync(filter.Field, cancellationToken));
        }

        return fields;
    }

    private static ResolvedSelectFieldPlan MapSelectField(
        ResolvedField field,
        IReadOnlyDictionary<string, string> entityAliases)
    {
        return new ResolvedSelectFieldPlan
        {
            SemanticKey = field.SemanticKey,
            EntityKey = field.EntityKey,
            EntityAlias = entityAliases[field.EntityKey],
            FieldKey = field.FieldKey,
            DisplayName = field.FieldDisplayName,
            DataType = field.DataType,
            PhysicalColumnName = field.PhysicalColumnName ?? string.Empty,
            SqlQualifiedName = field.SqlQualifiedName,
            OutputAlias = ToOutputAlias(field.FieldDisplayName, field.FieldKey)
        };
    }

    private static ResolvedMetricPlan MapMetric(
        QueryMetricDefinition metric,
        ResolvedField field,
        IReadOnlyDictionary<string, string> entityAliases)
    {
        return new ResolvedMetricPlan
        {
            MetricKey = metric.MetricKey,
            Function = metric.Function.ToString(),
            FieldSemanticKey = field.SemanticKey,
            EntityKey = field.EntityKey,
            EntityAlias = entityAliases[field.EntityKey],
            FieldKey = field.FieldKey,
            DataType = field.DataType,
            PhysicalColumnName = field.PhysicalColumnName ?? string.Empty,
            SqlQualifiedName = field.SqlQualifiedName,
            Alias = metric.Alias
        };
    }

    private static ResolvedFilterPlan MapFilter(
        QueryFilterDefinition filter,
        ResolvedField field,
        IReadOnlyDictionary<string, string> entityAliases)
    {
        return new ResolvedFilterPlan
        {
            FieldSemanticKey = field.SemanticKey,
            EntityKey = field.EntityKey,
            EntityAlias = entityAliases[field.EntityKey],
            FieldKey = field.FieldKey,
            DataType = field.DataType,
            PhysicalColumnName = field.PhysicalColumnName ?? string.Empty,
            Operator = filter.Operator.ToString(),
            Value = filter.Value
        };
    }

    private static ResolvedGroupByPlan MapGroupBy(
        ResolvedField field,
        IReadOnlyDictionary<string, string> entityAliases)
    {
        return new ResolvedGroupByPlan
        {
            SemanticKey = field.SemanticKey,
            EntityKey = field.EntityKey,
            EntityAlias = entityAliases[field.EntityKey],
            FieldKey = field.FieldKey,
            PhysicalColumnName = field.PhysicalColumnName ?? string.Empty,
            SqlQualifiedName = field.SqlQualifiedName
        };
    }

    private static ResolvedSortPlan MapSort(
        QuerySortDefinition sort,
        IReadOnlyList<ResolvedSelectFieldPlan> selectFields,
        IReadOnlyList<ResolvedMetricPlan> metrics)
    {
        var isMetricAlias = metrics.Any(metric => string.Equals(metric.Alias, sort.Field, StringComparison.OrdinalIgnoreCase));
        var isSelectField = selectFields.Any(field =>
            string.Equals(field.OutputAlias, sort.Field, StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.SemanticKey, sort.Field, StringComparison.OrdinalIgnoreCase));

        return new ResolvedSortPlan
        {
            Field = sort.Field,
            Direction = sort.Direction.ToString(),
            IsMetricAlias = isMetricAlias,
            IsSelectField = isSelectField
        };
    }

    private async Task<List<JoinPlan>> ResolveJoinsAsync(
        string baseEntityKey,
        IReadOnlyList<string> requiredEntityKeys,
        Dictionary<string, string> entityAliases,
        CancellationToken cancellationToken)
    {
        var joins = new List<JoinPlan>();
        var seenJoinSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var targetEntityKey in requiredEntityKeys)
        {
            if (string.Equals(baseEntityKey, targetEntityKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relationshipPath = await _relationshipResolver.ResolvePathAsync(
                baseEntityKey,
                targetEntityKey,
                cancellationToken);

            foreach (var join in relationshipPath.Joins)
            {
                if (seenJoinSignatures.Add(join.JoinSignature))
                {
                    joins.Add(await EnrichJoinAsync(join, entityAliases, cancellationToken));
                }
            }
        }

        _logger.LogInformation(
            "Built query join set with {JoinCount} joins from base entity {BaseEntityKey}.",
            joins.Count,
            baseEntityKey);

        return joins;
    }

    private async Task<JoinPlan> EnrichJoinAsync(
        JoinPlan join,
        Dictionary<string, string> entityAliases,
        CancellationToken cancellationToken)
    {
        var parentField = await _semanticResolver.ResolveFieldAsync(
            $"{join.ParentEntityKey}.{join.ParentFieldKey}",
            cancellationToken);
        var childField = await _semanticResolver.ResolveFieldAsync(
            $"{join.ChildEntityKey}.{join.ChildFieldKey}",
            cancellationToken);

        return new JoinPlan
        {
            ParentEntityKey = join.ParentEntityKey,
            ParentAlias = GetOrCreateEntityAlias(join.ParentEntityKey, entityAliases),
            ParentPhysicalSchemaName = parentField.PhysicalSchemaName ?? string.Empty,
            ParentPhysicalTableName = parentField.PhysicalTableName ?? string.Empty,
            ParentPhysicalColumnName = parentField.PhysicalColumnName ?? string.Empty,
            ChildEntityKey = join.ChildEntityKey,
            ChildAlias = GetOrCreateEntityAlias(join.ChildEntityKey, entityAliases),
            ChildPhysicalSchemaName = childField.PhysicalSchemaName ?? string.Empty,
            ChildPhysicalTableName = childField.PhysicalTableName ?? string.Empty,
            ChildPhysicalColumnName = childField.PhysicalColumnName ?? string.Empty,
            ParentFieldKey = join.ParentFieldKey,
            ChildFieldKey = join.ChildFieldKey,
            JoinType = join.JoinType,
            Cardinality = join.Cardinality,
            IsRequired = join.IsRequired
        };
    }

    private static IReadOnlyList<string> GetRequiredEntityKeys(
        params IEnumerable<ResolvedField>[] fieldGroups)
    {
        var entityKeys = new List<string>();
        var seenEntityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldGroup in fieldGroups)
        {
            foreach (var field in fieldGroup)
            {
                if (seenEntityKeys.Add(field.EntityKey))
                {
                    entityKeys.Add(field.EntityKey);
                }
            }
        }

        return entityKeys;
    }

    private static string GetEntityAlias(string entityKey)
    {
        return entityKey switch
        {
            "patient" => "p",
            "clinical_study" => "cs",
            "study_site" => "ss",
            "visit" => "v",
            "lab_result" => "lr",
            _ => "t1"
        };
    }

    private static Dictionary<string, string> BuildEntityAliases(
        string baseEntityKey,
        IReadOnlyList<string> requiredEntityKeys)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [baseEntityKey] = GetEntityAlias(baseEntityKey)
        };
        var fallbackIndex = 1;

        foreach (var entityKey in requiredEntityKeys)
        {
            if (aliases.ContainsKey(entityKey))
            {
                continue;
            }

            var alias = GetEntityAlias(entityKey);
            if (alias == "t1")
            {
                do
                {
                    alias = $"t{fallbackIndex++}";
                }
                while (aliases.ContainsValue(alias));
            }

            aliases[entityKey] = alias;
        }

        return aliases;
    }

    private static string GetOrCreateEntityAlias(
        string entityKey,
        Dictionary<string, string> aliases)
    {
        if (aliases.TryGetValue(entityKey, out var existingAlias))
        {
            return existingAlias;
        }

        var alias = GetEntityAlias(entityKey);
        if (alias == "t1")
        {
            var fallbackIndex = 1;
            do
            {
                alias = $"t{fallbackIndex++}";
            }
            while (aliases.ContainsValue(alias));
        }

        aliases[entityKey] = alias;

        return alias;
    }

    private static string ToOutputAlias(string displayName, string fieldKey)
    {
        var source = string.IsNullOrWhiteSpace(displayName)
            ? fieldKey
            : displayName;
        var parts = source
            .Replace("_", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "Field";
        }

        return string.Concat(parts.Select(ToPascalPart));
    }

    private static string ToPascalPart(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        return char.ToUpper(value[0], CultureInfo.InvariantCulture) + value[1..];
    }
}
