using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.Interfaces;

public interface IReportQueryValidator
{
    Task ValidateAsync(
        ReportQueryRequest request,
        CancellationToken cancellationToken = default);

    ReportQueryRequest Normalize(ReportQueryRequest request);
}
