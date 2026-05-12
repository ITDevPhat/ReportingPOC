using Microsoft.Extensions.DependencyInjection;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Application.Resolvers;

namespace ReportingPlatform.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISemanticResolver, SemanticResolver>();
        services.AddScoped<IRelationshipGraphProvider, RelationshipGraphProvider>();
        services.AddScoped<IRelationshipResolver, RelationshipResolver>();

        return services;
    }
}
