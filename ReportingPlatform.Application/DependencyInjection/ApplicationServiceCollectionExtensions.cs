using Microsoft.Extensions.DependencyInjection;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Application.QueryPlanning;
using ReportingPlatform.Application.Resolvers;
using ReportingPlatform.Application.Validation;

namespace ReportingPlatform.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISemanticResolver, SemanticResolver>();
        services.AddScoped<IRelationshipGraphProvider, RelationshipGraphProvider>();
        services.AddScoped<IRelationshipResolver, RelationshipResolver>();
        services.AddScoped<IReportQueryValidator, ReportQueryValidator>();
        services.AddScoped<IQueryPlanBuilder, QueryPlanBuilder>();

        return services;
    }
}
