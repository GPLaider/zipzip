using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ZipZip.App.Visuals;

public static class ArchiveFormatAssetCatalog
{
    private static readonly string[] OpenArchiveExtensions =
    [
        ".zip",
        ".7z",
        ".rar",
        ".tar",
        ".gz",
        ".bz2",
        ".xz",
        ".lz",
        ".lzma",
        ".zst",
        ".tgz",
        ".tbz",
        ".tbz2",
        ".txz",
        ".cab",
        ".iso",
        ".wim",
        ".arj",
        ".cpio",
        ".z",
        ".lzh"
    ];

    public static string BrandImageUri => "ms-appx:///Assets/App/ZipZip.png";

    public static IReadOnlyList<string> SupportedOpenArchiveExtensions => OpenArchiveExtensions;

    public static ImageSource GetImageSource(string? formatOrPath)
    {
        var image = new BitmapImage
        {
            UriSource = new Uri(GetImageUri(formatOrPath)),
        };

        return image;
    }

    public static string GetImageUri(string? formatOrPath)
    {
        return $"ms-appx:///Assets/FileTypes/{ResolveAssetKey(formatOrPath)}.png";
    }

    public static string GetBrandIconPath(string appExecutablePath)
    {
        var baseDirectory = Path.GetDirectoryName(appExecutablePath) ?? AppContext.BaseDirectory;
        var iconPath = Path.Combine(baseDirectory, "Assets", "App", "ZipZip.ico");
        return File.Exists(iconPath) ? iconPath : appExecutablePath;
    }

    public static string GetShellIconPath(string appExecutablePath, string? formatOrPath)
    {
        var baseDirectory = Path.GetDirectoryName(appExecutablePath) ?? AppContext.BaseDirectory;
        var iconPath = Path.Combine(baseDirectory, "Assets", "FileTypes", $"{ResolveAssetKey(formatOrPath)}.ico");

        if (File.Exists(iconPath))
        {
            return iconPath;
        }

        return GetBrandIconPath(appExecutablePath);
    }

    public static bool IsKnownArchiveFormat(string? formatOrPath)
    {
        return ResolveAssetKey(formatOrPath) is not "default";
    }

    public static bool IsSupportedOpenArchivePath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && File.Exists(path)
            && IsKnownArchiveFormat(path);
    }

    public static string GetDisplayLabel(string? formatOrPath)
    {
        var value = formatOrPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "ARCHIVE";
        }

        var lower = value.ToLowerInvariant();

        if (lower.EndsWith(".tar.gz", StringComparison.Ordinal) || lower.EndsWith(".tgz", StringComparison.Ordinal))
        {
            return "TGZ";
        }

        if (lower.EndsWith(".tar.bz2", StringComparison.Ordinal)
            || lower.EndsWith(".tbz", StringComparison.Ordinal)
            || lower.EndsWith(".tbz2", StringComparison.Ordinal))
        {
            return "TBZ2";
        }

        if (lower.EndsWith(".tar.xz", StringComparison.Ordinal) || lower.EndsWith(".txz", StringComparison.Ordinal))
        {
            return "TXZ";
        }

        if (lower.EndsWith(".tar.zst", StringComparison.Ordinal) || lower.EndsWith(".tzst", StringComparison.Ordinal))
        {
            return "TZST";
        }

        var extension = Path.GetExtension(value).TrimStart('.').ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension;
        }

        return ResolveAssetKey(value) switch
        {
            "zip" => "ZIP",
            "7z" => "7Z",
            "rar" => "RAR",
            "tar" => "TAR",
            "tgz" => "TGZ",
            "bz2" => "BZ2",
            "xz" => "XZ",
            "zst" => "ZST",
            "cab" => "CAB",
            "iso" => "ISO",
            "alz" => "ALZ",
            _ => value.ToUpperInvariant(),
        };
    }

    public static string ResolveAssetKey(string? formatOrPath)
    {
        var value = formatOrPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "default";
        }

        var lower = value.ToLowerInvariant();

        if (lower.EndsWith(".tar.gz", StringComparison.Ordinal)
            || lower.EndsWith(".tgz", StringComparison.Ordinal)
            || lower is "tar.gz" or "tgz" or "gz")
        {
            return "tgz";
        }

        if (lower.EndsWith(".tar.bz2", StringComparison.Ordinal)
            || lower.EndsWith(".tbz", StringComparison.Ordinal)
            || lower.EndsWith(".tbz2", StringComparison.Ordinal)
            || lower is "bz2" or "tbz" or "tbz2")
        {
            return "bz2";
        }

        if (lower.EndsWith(".tar.xz", StringComparison.Ordinal)
            || lower.EndsWith(".txz", StringComparison.Ordinal)
            || lower is "xz" or "txz")
        {
            return "xz";
        }

        if (lower.EndsWith(".tar.zst", StringComparison.Ordinal)
            || lower.EndsWith(".tzst", StringComparison.Ordinal)
            || lower is "zst" or "tzst")
        {
            return "zst";
        }

        return lower switch
        {
            var item when item.EndsWith(".zip", StringComparison.Ordinal) || item == "zip" => "zip",
            var item when item.EndsWith(".7z", StringComparison.Ordinal) || item is "7z" or "sevenzip" => "7z",
            var item when item.EndsWith(".rar", StringComparison.Ordinal) || item == "rar" => "rar",
            var item when item.EndsWith(".tar", StringComparison.Ordinal) || item == "tar" => "tar",
            var item when item.EndsWith(".cab", StringComparison.Ordinal)
                || item.EndsWith(".wim", StringComparison.Ordinal)
                || item.EndsWith(".arj", StringComparison.Ordinal)
                || item.EndsWith(".cpio", StringComparison.Ordinal)
                || item.EndsWith(".z", StringComparison.Ordinal)
                || item.EndsWith(".lzh", StringComparison.Ordinal)
                || item.EndsWith(".lz", StringComparison.Ordinal)
                || item.EndsWith(".lzma", StringComparison.Ordinal)
                || item is "cab" or "wim" or "arj" or "cpio" or "z" or "lzh" or "lz" or "lzma" => "cab",
            var item when item.EndsWith(".iso", StringComparison.Ordinal) || item == "iso" => "iso",
            var item when item.EndsWith(".alz", StringComparison.Ordinal) || item == "alz" => "alz",
            _ => "default",
        };
    }
}
