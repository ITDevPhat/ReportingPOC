using ReportingPlatform.Application.Interfaces;

namespace ReportingPlatform.Api.Services;

public sealed class ReportExecutionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportExecutionWorker> _logger;

    public ReportExecutionWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ReportExecutionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = GetInt("ReportingEngine:BackgroundWorkerIntervalSeconds", 3);
        var batchSize = GetInt("ReportingEngine:BackgroundWorkerBatchSize", 5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(batchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Report execution worker failed while processing pending executions.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var executionRepository = scope.ServiceProvider.GetRequiredService<IReportExecutionRepository>();
        var executionService = scope.ServiceProvider.GetRequiredService<IReportExecutionService>();
        var pendingExecutionIds = await executionRepository.GetPendingExecutionIdsAsync(batchSize, cancellationToken);

        foreach (var executionId in pendingExecutionIds)
        {
            try
            {
                await executionService.ProcessExecutionAsync(executionId, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process report execution {ExecutionId}.", executionId);
            }
        }
    }

    private int GetInt(string key, int fallback)
    {
        return int.TryParse(_configuration[key], out var value)
            ? value
            : fallback;
    }
}
