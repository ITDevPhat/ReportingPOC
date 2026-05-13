using ReportingPlatform.Application.Interfaces;
using ReportingPlatform.Domain.Exports;

namespace ReportingPlatform.Infrastructure.Storage;

public sealed class LocalArtifactStorage : IArtifactStorage
{
    private readonly string _rootPath;

    public LocalArtifactStorage(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var configuredPath = configuration["ArtifactStorage:LocalPath"];
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath) ? "artifacts" : configuredPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<ExportResult> SaveAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = $"report-{DateTime.UtcNow:yyyyMMddHHmmss}.dat";
        }

        var now = DateTime.UtcNow;
        var artifactKey = string.Join(
            '/',
            "reports",
            now.Year.ToString("0000"),
            now.Month.ToString("00"),
            now.Day.ToString("00"),
            Guid.NewGuid().ToString("N"),
            safeFileName);

        var filePath = ResolvePath(artifactKey, mustExist: false);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using (var fileStream = File.Create(filePath))
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var info = new FileInfo(filePath);

        return new ExportResult
        {
            ArtifactKey = artifactKey,
            FileName = safeFileName,
            ContentType = contentType,
            SizeBytes = info.Length,
            CreatedAt = now,
            DownloadUrl = await GetDownloadUrlAsync(artifactKey, TimeSpan.FromMinutes(30), cancellationToken)
        };
    }

    public Task<Stream> OpenReadAsync(string artifactKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = ResolvePath(artifactKey, mustExist: true);
        Stream stream = File.OpenRead(filePath);

        return Task.FromResult(stream);
    }

    public Task<string> GetDownloadUrlAsync(
        string artifactKey,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult($"/api/report-exports/download/{Uri.EscapeDataString(artifactKey)}");
    }

    private string ResolvePath(string artifactKey, bool mustExist)
    {
        if (string.IsNullOrWhiteSpace(artifactKey)
            || Path.IsPathRooted(artifactKey)
            || artifactKey.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid artifact key.");
        }

        var normalizedKey = artifactKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));
        var rootWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid artifact key.");
        }

        if (mustExist && !File.Exists(fullPath))
        {
            throw new FileNotFoundException("Artifact was not found.", artifactKey);
        }

        return fullPath;
    }
}
