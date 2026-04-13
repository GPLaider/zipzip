using ZipZip.Domain.Models;
using ZipZip.Domain.Services;

namespace ZipZip.Application.UseCases;

public sealed class ExtractArchiveUseCase
{
    private readonly IArchiveBackend _archiveBackend;

    public ExtractArchiveUseCase(IArchiveBackend archiveBackend)
    {
        _archiveBackend = archiveBackend;
    }

    public Task ExecuteAsync(
        string archivePath,
        ExtractionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            throw new ArgumentException("Archive path is required.", nameof(archivePath));
        }

        return _archiveBackend.ExtractAsync(archivePath, options, cancellationToken);
    }
}
