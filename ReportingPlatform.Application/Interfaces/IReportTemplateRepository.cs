using ReportingPlatform.Application.DTOs.Reports;

namespace ReportingPlatform.Application.Interfaces;

public interface IReportTemplateRepository
{
    Task<Guid> CreateAsync(CreateReportTemplateRequest request, string? createdBy, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid templateId, UpdateReportTemplateRequest request, string? updatedBy, CancellationToken cancellationToken = default);

    Task<ReportTemplateDto?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<ReportTemplateDto?> GetByKeyAsync(string templateKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportTemplateDto>> ListAsync(CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid templateId, string? updatedBy, CancellationToken cancellationToken = default);
}
