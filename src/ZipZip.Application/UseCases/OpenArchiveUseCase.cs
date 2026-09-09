using ZipZip.Domain.Models;
using ZipZip.Domain.Services;

namespace ZipZip.Application.UseCases;

public sealed class OpenArchiveUseCase
{
    private readonly IArchiveBackend _archiveBackend;

    public OpenArchiveUseCase(IArchiveBackend archiveBackend)
    {
        _archiveBackend = archiveBackend;
    }

    public Task<ArchiveSummary> ExecuteAsync(string archivePath, CancellationToken cancellationToken = default, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            throw new ArgumentException("Archive path is required.", nameof(archivePath));
        }

        return _archiveBackend.OpenAsync(archivePath, cancellationToken, password);
    }
}
