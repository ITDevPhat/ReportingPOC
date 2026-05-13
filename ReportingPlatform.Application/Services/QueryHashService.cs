using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Query;

namespace ReportingPlatform.Application.Services;

public sealed class QueryHashService : IQueryHashService
{
    private readonly IReportQueryValidator _queryValidator;

    public QueryHashService(IReportQueryValidator queryValidator)
    {
        _queryValidator = queryValidator;
    }

    public string ComputeHash(ReportQueryRequest request)
    {
        var normalizedRequest = _queryValidator.Normalize(request);
        var json = JsonSerializer.Serialize(normalizedRequest, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
