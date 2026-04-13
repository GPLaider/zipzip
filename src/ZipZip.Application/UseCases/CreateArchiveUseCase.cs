using ZipZip.Domain.Models;
using ZipZip.Domain.Services;

namespace ZipZip.Application.UseCases;

public sealed class CreateArchiveUseCase
{
    private readonly IArchiveBackend _archiveBackend;

    public CreateArchiveUseCase(IArchiveBackend archiveBackend)
    {
        _archiveBackend = archiveBackend;
    }

    public Task ExecuteAsync(
        IReadOnlyList<string> inputPaths,
        CompressionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (inputPaths.Count == 0)
        {
            throw new ArgumentException("At least one input path is required.", nameof(inputPaths));
        }

        return _archiveBackend.CreateAsync(inputPaths, options, cancellationToken);
    }
}
