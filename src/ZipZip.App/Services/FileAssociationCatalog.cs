namespace ZipZip.App.Services;

public sealed record ArchiveAssociationOption(string Extension, string Label);

public static class FileAssociationCatalog
{
    public static IReadOnlyList<ArchiveAssociationOption> ArchiveFileOptions { get; } =
    [
        new(".zip", "ZIP"),
        new(".7z", "7Z"),
        new(".rar", "RAR"),
        new(".cab", "CAB"),
        new(".arj", "ARJ"),
        new(".lzh", "LZH"),
    ];

    public static IReadOnlyList<ArchiveAssociationOption> UnixArchiveFileOptions { get; } =
    [
        new(".tar", "TAR"),
        new(".gz", "GZ"),
        new(".tgz", "TGZ"),
        new(".bz2", "BZ2"),
        new(".tbz", "TBZ"),
        new(".tbz2", "TBZ2"),
        new(".xz", "XZ"),
        new(".txz", "TXZ"),
        new(".zst", "ZST"),
        new(".lz", "LZ"),
        new(".lzma", "LZMA"),
        new(".z", "Z"),
        new(".cpio", "CPIO"),
    ];

    public static IReadOnlyList<ArchiveAssociationOption> DiskImageOptions { get; } =
    [
        new(".iso", "ISO"),
        new(".wim", "WIM"),
    ];

    public static IReadOnlyList<string> RecommendedExtensions { get; } =
    [
        ".zip",
        ".7z",
        ".rar",
        ".tar",
        ".gz",
        ".bz2",
        ".xz",
        ".zst",
        ".tgz",
        ".tbz",
        ".tbz2",
        ".txz",
        ".cab",
        ".iso",
        ".wim",
    ];

    public static IReadOnlyList<string> AllExtensions { get; } =
        ArchiveFileOptions
            .Concat(UnixArchiveFileOptions)
            .Concat(DiskImageOptions)
            .Select(option => option.Extension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static string GetLabel(string extension)
    {
        return ArchiveFileOptions
            .Concat(UnixArchiveFileOptions)
            .Concat(DiskImageOptions)
            .FirstOrDefault(option => string.Equals(option.Extension, extension, StringComparison.OrdinalIgnoreCase))
            ?.Label
            ?? extension.TrimStart('.').ToUpperInvariant();
    }
}
