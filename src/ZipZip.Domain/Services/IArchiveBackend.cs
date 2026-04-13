using ZipZip.Domain.Models;

namespace ZipZip.Domain.Services;

public interface IArchiveBackend
{
    Task<ArchiveSummary> OpenAsync(string archivePath, CancellationToken cancellationToken = default);
    Task ExtractAsync(string archivePath, ExtractionOptions options, CancellationToken cancellationToken = default);
    Task CreateAsync(IReadOnlyList<string> inputPaths, CompressionOptions options, CancellationToken cancellationToken = default);
    Task TestAsync(string archivePath, CancellationToken cancellationToken = default);
}
