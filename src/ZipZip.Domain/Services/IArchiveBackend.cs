using ZipZip.Domain.Models;

namespace ZipZip.Domain.Services;

public interface IArchiveBackend
{
    Task<ArchiveSummary> OpenAsync(string archivePath, CancellationToken cancellationToken = default, string? password = null);
    Task ExtractAsync(string archivePath, ExtractionOptions options, CancellationToken cancellationToken = default, IProgress<int>? progress = null);
    Task CreateAsync(IReadOnlyList<string> inputPaths, CompressionOptions options, CancellationToken cancellationToken = default, IProgress<int>? progress = null);
    Task TestAsync(string archivePath, CancellationToken cancellationToken = default, string? password = null, IProgress<int>? progress = null);
}
