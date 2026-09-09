using ZipZip.Domain.Services;

namespace ZipZip.Application.UseCases;

public sealed class TestArchiveUseCase(IArchiveBackend archiveBackend)
{
    public Task ExecuteAsync(string archivePath, CancellationToken cancellationToken = default,
        string? password = null, IProgress<int>? progress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        return archiveBackend.TestAsync(archivePath, cancellationToken, password, progress);
    }
}
