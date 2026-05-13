using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Infrastructure.Caching;
using ReportingPlatform.Infrastructure.Execution;
using ReportingPlatform.Infrastructure.Exports;
using ReportingPlatform.Infrastructure.Metadata;
using ReportingPlatform.Infrastructure.Persistence;
using ReportingPlatform.Infrastructure.Repositories;
using ReportingPlatform.Infrastructure.Storage;

namespace ReportingPlatform.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddMemoryCache();
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ISemanticMetadataProvider, SqlSemanticMetadataProvider>();
        services.AddScoped<IQueryExecutor, SqlServerQueryExecutor>();
        services.AddScoped<IReportTemplateRepository, ReportTemplateRepository>();
        services.AddScoped<IReportExecutionRepository, ReportExecutionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IQueryResultCache, MemoryQueryResultCache>();
        services.AddScoped<IArtifactStorage, LocalArtifactStorage>();
        services.AddScoped<CsvReportExporter>();
        services.AddScoped<ExcelReportExporter>();
        services.AddScoped<IReportExporter, ReportExportService>();

        return services;
    }
}
