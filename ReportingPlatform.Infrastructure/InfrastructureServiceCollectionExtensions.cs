using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Infrastructure.Execution;
using ReportingPlatform.Infrastructure.Metadata;
using ReportingPlatform.Infrastructure.Persistence;

namespace ReportingPlatform.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ISemanticMetadataProvider, SqlSemanticMetadataProvider>();
        services.AddScoped<IQueryExecutor, SqlServerQueryExecutor>();

        return services;
    }
}
