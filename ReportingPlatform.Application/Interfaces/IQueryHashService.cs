using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.Interfaces;

public interface IQueryHashService
{
    string ComputeHash(ReportQueryRequest request);
}
