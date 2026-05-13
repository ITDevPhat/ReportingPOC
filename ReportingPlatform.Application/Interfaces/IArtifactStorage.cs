using ReportingPlatform.Domain.Exports;

namespace ReportingPlatform.Application.Interfaces;

public interface IArtifactStorage
{
    Task<ExportResult> SaveAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string artifactKey, CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(string artifactKey, TimeSpan expiration, CancellationToken cancellationToken = default);
}
