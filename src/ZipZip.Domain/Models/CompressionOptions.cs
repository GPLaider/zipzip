namespace ZipZip.Domain.Models;

public sealed record CompressionOptions(
    string OutputPath,
    ArchiveFormat Format,
    CompressionLevel Level,
    string? Password = null,
    string? SplitSize = null,
    bool EncryptFileNames = false);
