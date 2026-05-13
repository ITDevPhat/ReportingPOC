using Microsoft.Extensions.Logging;
using ReportingPlatform.Application.DTOs.Reports;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Reports;

namespace ReportingPlatform.Application.Services;

public sealed class ReportExecutionService : IReportExecutionService
{
    private readonly IReportExecutionRepository _executionRepository;
    private readonly IReportTemplateRepository _templateRepository;
    private readonly IQueryExecutor _queryExecutor;
    private readonly ILogger<ReportExecutionService> _logger;

    public ReportExecutionService(
        IReportExecutionRepository executionRepository,
        IReportTemplateRepository templateRepository,
        IQueryExecutor queryExecutor,
        ILogger<ReportExecutionService> logger)
    {
        _executionRepository = executionRepository;
        _templateRepository = templateRepository;
        _queryExecutor = queryExecutor;
        _logger = logger;
    }

    public async Task<Guid> SubmitAsync(RunReportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Query is null && request.TemplateId is null)
        {
            throw new ArgumentException("Either query or templateId must be provided.");
        }

        var query = request.Query;
        if (request.TemplateId is not null)
        {
            var template = await _templateRepository.GetByIdAsync(request.TemplateId.Value, cancellationToken)
                ?? throw new ArgumentException($"Report template '{request.TemplateId}' was not found.");

            if (!template.IsActive)
            {
                throw new ArgumentException($"Report template '{request.TemplateId}' is inactive.");
            }

            query = template.Query;
        }

        var executionId = await _executionRepository.CreatePendingAsync(
            new RunReportRequest
            {
                TemplateId = request.TemplateId,
                Query = query,
                CreatedBy = request.CreatedBy
            },
            cancellationToken);

        _logger.LogInformation(
            "Submitted report execution {ExecutionId} for template {TemplateId}.",
            executionId,
            request.TemplateId);

        return executionId;
    }

    public Task<ReportExecutionDto?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return _executionRepository.GetByIdAsync(executionId, cancellationToken);
    }

    public async Task ProcessExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
        if (execution is null)
        {
            throw new ArgumentException($"Report execution '{executionId}' was not found.");
        }

        if (!string.Equals(execution.Status, ReportExecutionStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _executionRepository.MarkProcessingAsync(executionId, cancellationToken);
        _logger.LogInformation("Report execution {ExecutionId} marked Processing.", executionId);

        try
        {
            var result = await _queryExecutor.ExecuteAsync(execution.Query, cancellationToken);
            if (result.Success)
            {
                await _executionRepository.MarkCompletedAsync(executionId, result, cancellationToken);
                _logger.LogInformation(
                    "Report execution {ExecutionId} completed with {RowCount} rows.",
                    executionId,
                    result.RowCount);
                return;
            }

            await _executionRepository.MarkFailedAsync(
                executionId,
                result.Error ?? "Report execution failed.",
                cancellationToken);
            _logger.LogWarning("Report execution {ExecutionId} failed: {Error}", executionId, result.Error);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to process report execution {ExecutionId}.", executionId);
            await _executionRepository.MarkFailedAsync(executionId, "Report execution failed.", cancellationToken);
        }
    }
}
