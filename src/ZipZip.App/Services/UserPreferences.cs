using ZipZip.Application.Theme;
using ZipZip.Domain.Models;

namespace ZipZip.App.Services;

public sealed record UserPreferences(
    ThemeMode ThemeMode,
    ArchiveFormat DefaultArchiveFormat,
    CompressionLevel DefaultCompressionLevel,
    string? LastCompressionOutputDirectory,
    IReadOnlyList<RecentArchivePreference> RecentArchives,
    IReadOnlyList<string>? AssociatedArchiveExtensions,
    ShellMenuPreferences? ShellMenus)
{
    public static UserPreferences Default { get; } =
        new(
            ThemeMode.System,
            ArchiveFormat.Zip,
            CompressionLevel.Normal,
            null,
            [],
            null,
            null);
}

public sealed record RecentArchivePreference(
    string FileName,
    string FullPath,
    DateTimeOffset LastOpenedAt);

public sealed record ShellMenuPreferences(
    bool ShowOpenWithZipZip,
    bool ShowExtractHere,
    bool ShowExtractNewFolder,
    bool ShowCompressDialog,
    bool ShowCompressZip,
    bool ShowCompressSevenZip)
{
    public static ShellMenuPreferences Default { get; } =
        new(
            ShowOpenWithZipZip: true,
            ShowExtractHere: true,
            ShowExtractNewFolder: true,
            ShowCompressDialog: true,
            ShowCompressZip: true,
            ShowCompressSevenZip: true);
}
