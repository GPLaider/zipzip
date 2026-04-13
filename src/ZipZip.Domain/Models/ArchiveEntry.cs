namespace ZipZip.Domain.Models;

public sealed class ArchiveEntry
{
    public string Name { get; set; } = string.Empty;

    public string TypeLabel { get; set; } = string.Empty;

    public long PackedSize { get; set; }

    public long OriginalSize { get; set; }

    public bool IsDirectory { get; set; }

    public string? Path { get; set; }
}
