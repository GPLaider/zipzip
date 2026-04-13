namespace ZipZip.Domain.Models;

public sealed record ArchiveSummary(
    string FilePath,
    ArchiveFormat Format,
    IReadOnlyList<ArchiveEntry> Entries,
    bool IsEncrypted = false,
    bool IsSplitArchive = false);
