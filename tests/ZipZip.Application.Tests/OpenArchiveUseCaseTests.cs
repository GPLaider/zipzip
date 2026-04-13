using ZipZip.Application.UseCases;
using ZipZip.Domain.Models;
using ZipZip.Domain.Services;

namespace ZipZip.Application.Tests;

public sealed class OpenArchiveUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Passes_Password_To_Backend()
    {
        var backend = new FakeArchiveBackend();
        var useCase = new OpenArchiveUseCase(backend);

        await useCase.ExecuteAsync(@"C:\Temp\secret.7z", "secret123");

        Assert.Equal(@"C:\Temp\secret.7z", backend.LastArchivePath);
        Assert.Equal("secret123", backend.LastPassword);
    }

    private sealed class FakeArchiveBackend : IArchiveBackend
    {
        public string? LastArchivePath { get; private set; }

        public string? LastPassword { get; private set; }

        public Task<ArchiveSummary> OpenAsync(string archivePath, string? password = null, CancellationToken cancellationToken = default)
        {
            LastArchivePath = archivePath;
            LastPassword = password;
            return Task.FromResult(new ArchiveSummary(archivePath, ArchiveFormat.SevenZip, []));
        }

        public Task ExtractAsync(string archivePath, ExtractionOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task CreateAsync(IReadOnlyList<string> inputPaths, CompressionOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task TestAsync(string archivePath, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
