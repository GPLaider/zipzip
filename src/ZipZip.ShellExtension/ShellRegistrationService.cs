using Microsoft.Win32;

namespace ZipZip.ShellExtension;

public sealed class ShellRegistrationService
{
    public sealed record ShellMenuRegistrationOptions(
        bool ShowOpenWithZipZip,
        bool ShowExtractHere,
        bool ShowExtractNewFolder,
        bool ShowCompressDialog,
        bool ShowCompressZip,
        bool ShowCompressSevenZip)
    {
        public static ShellMenuRegistrationOptions Default { get; } =
            new(
                ShowOpenWithZipZip: true,
                ShowExtractHere: true,
                ShowExtractNewFolder: true,
                ShowCompressDialog: true,
                ShowCompressZip: true,
                ShowCompressSevenZip: true);
    }

    private static readonly string[] ArchiveExtensions =
    [
        ".zip", ".7z", ".rar", ".tar", ".gz", ".bz2", ".xz", ".lz", ".lzma", ".zst",
        ".tgz", ".tbz", ".tbz2", ".txz", ".cab", ".iso", ".wim", ".arj", ".cpio", ".z", ".lzh"
    ];

    private static readonly string[] CompressionCommandKeys =
    [
        "ZipZipCompressDialog",
        "ZipZipCompressZip",
        "ZipZipCompress7Z",
        "ZipZip",
    ];

    private static readonly string[] ArchiveCommandKeys =
    [
        "ZipZipOpen",
        "ZipZipExtractHere",
        "ZipZipExtractNewFolder",
        "ZipZip",
    ];

    public void Register(
        string appExecutablePath,
        string shellHelperPath,
        ShellMenuRegistrationOptions? options = null)
    {
        if (!File.Exists(appExecutablePath))
        {
            throw new FileNotFoundException("\uC2E4\uD589 \uD30C\uC77C\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", appExecutablePath);
        }

        if (!File.Exists(shellHelperPath))
        {
            throw new FileNotFoundException("\uC178 \uB3C4\uC6B0\uBBF8 \uD30C\uC77C\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", shellHelperPath);
        }

        Unregister();

        var effectiveOptions = options ?? ShellMenuRegistrationOptions.Default;
        RegisterCompressionMenu(appExecutablePath, shellHelperPath, effectiveOptions);

        if (effectiveOptions.ShowOpenWithZipZip
            || effectiveOptions.ShowExtractHere
            || effectiveOptions.ShowExtractNewFolder)
        {
            foreach (var extension in ArchiveExtensions)
            {
                RegisterArchiveMenu(appExecutablePath, shellHelperPath, extension, effectiveOptions);
            }
        }
    }

    public void Unregister()
    {
        using var classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes");
        if (classes is null)
        {
            return;
        }

        foreach (var commandKey in CompressionCommandKeys)
        {
            classes.DeleteSubKeyTree($@"AllFilesystemObjects\shell\{commandKey}", throwOnMissingSubKey: false);
        }

        foreach (var extension in ArchiveExtensions)
        {
            foreach (var commandKey in ArchiveCommandKeys)
            {
                classes.DeleteSubKeyTree($@"SystemFileAssociations\{extension}\shell\{commandKey}", throwOnMissingSubKey: false);
            }
        }
    }

    private static void RegisterCompressionMenu(
        string appExecutablePath,
        string shellHelperPath,
        ShellMenuRegistrationOptions options)
    {
        if (options.ShowCompressDialog)
        {
            CreateTopLevelCommand(
                @"Software\Classes\AllFilesystemObjects\shell\ZipZipCompressDialog",
                "ZipZip\uC73C\uB85C \uC555\uCD95\uD558\uAE30...",
                BuildCommand(appExecutablePath, "--create \"%1\""),
                ResolveBrandIconPath(appExecutablePath),
                "Player");
        }

        if (options.ShowCompressZip)
        {
            CreateTopLevelCommand(
                @"Software\Classes\AllFilesystemObjects\shell\ZipZipCompressZip",
                "ZIP\uC73C\uB85C \uC555\uCD95\uD558\uAE30",
                BuildCommand(shellHelperPath, "compress-zip \"%1\""),
                ResolveIconPath(appExecutablePath, "zip"),
                "Player");
        }

        if (options.ShowCompressSevenZip)
        {
            CreateTopLevelCommand(
                @"Software\Classes\AllFilesystemObjects\shell\ZipZipCompress7Z",
                "7Z\uB85C \uC555\uCD95\uD558\uAE30",
                BuildCommand(shellHelperPath, "compress-7z \"%1\""),
                ResolveIconPath(appExecutablePath, "7z"),
                "Player");
        }
    }

    private static void RegisterArchiveMenu(
        string appExecutablePath,
        string shellHelperPath,
        string extension,
        ShellMenuRegistrationOptions options)
    {
        var iconPath = ResolveIconPath(appExecutablePath, extension);

        if (options.ShowOpenWithZipZip)
        {
            CreateTopLevelCommand(
                $@"Software\Classes\SystemFileAssociations\{extension}\shell\ZipZipOpen",
                "ZipZip\uC73C\uB85C \uC5F4\uAE30",
                BuildCommand(appExecutablePath, "\"%1\""),
                iconPath,
                "Single");
        }

        if (options.ShowExtractHere)
        {
            CreateTopLevelCommand(
                $@"Software\Classes\SystemFileAssociations\{extension}\shell\ZipZipExtractHere",
                "\uC5EC\uAE30\uC5D0 \uD480\uAE30",
                BuildCommand(shellHelperPath, "extract-here \"%1\""),
                iconPath,
                "Single");
        }

        if (options.ShowExtractNewFolder)
        {
            CreateTopLevelCommand(
                $@"Software\Classes\SystemFileAssociations\{extension}\shell\ZipZipExtractNewFolder",
                "\uC0C8 \uD3F4\uB354\uC5D0 \uD480\uAE30",
                BuildCommand(shellHelperPath, "extract-new-folder \"%1\""),
                iconPath,
                "Single");
        }
    }

    private static void CreateTopLevelCommand(
        string keyPath,
        string label,
        string commandLine,
        string iconPath,
        string selectionModel)
    {
        using var commandKey = Registry.CurrentUser.CreateSubKey(keyPath);
        using var executionKey = commandKey?.CreateSubKey("command");

        if (commandKey is null || executionKey is null)
        {
            throw new InvalidOperationException($"\uBA54\uB274 {keyPath} \uD0A4\uB97C \uB9CC\uB4E4\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.");
        }

        commandKey.SetValue("MUIVerb", label);
        commandKey.SetValue("Icon", iconPath);
        commandKey.SetValue("MultiSelectModel", selectionModel);
        executionKey.SetValue(string.Empty, commandLine);
    }

    private static string ResolveIconPath(string appExecutablePath, string formatOrExtension)
    {
        var baseDirectory = Path.GetDirectoryName(appExecutablePath) ?? AppContext.BaseDirectory;
        var assetName = ResolveAssetName(formatOrExtension);
        var assetPath = Path.Combine(baseDirectory, "Assets", "FileTypes", $"{assetName}.ico");

        return File.Exists(assetPath)
            ? assetPath
            : ResolveBrandIconPath(appExecutablePath);
    }

    private static string ResolveBrandIconPath(string appExecutablePath)
    {
        var baseDirectory = Path.GetDirectoryName(appExecutablePath) ?? AppContext.BaseDirectory;
        var brandPath = Path.Combine(baseDirectory, "Assets", "App", "ZipZip.ico");
        return File.Exists(brandPath) ? brandPath : appExecutablePath;
    }

    private static string ResolveAssetName(string formatOrExtension)
    {
        var value = formatOrExtension.Trim().ToLowerInvariant();

        if (value.EndsWith(".tar.gz", StringComparison.Ordinal)
            || value.EndsWith(".tgz", StringComparison.Ordinal)
            || value is "tgz" or "gz")
        {
            return "tgz";
        }

        if (value.EndsWith(".tar.bz2", StringComparison.Ordinal)
            || value.EndsWith(".tbz", StringComparison.Ordinal)
            || value.EndsWith(".tbz2", StringComparison.Ordinal)
            || value is "bz2" or "tbz" or "tbz2")
        {
            return "bz2";
        }

        if (value.EndsWith(".tar.xz", StringComparison.Ordinal)
            || value.EndsWith(".txz", StringComparison.Ordinal)
            || value is "xz" or "txz")
        {
            return "xz";
        }

        if (value.EndsWith(".tar.zst", StringComparison.Ordinal)
            || value.EndsWith(".tzst", StringComparison.Ordinal)
            || value is "zst" or "tzst")
        {
            return "zst";
        }

        return value switch
        {
            var item when item.EndsWith(".zip", StringComparison.Ordinal) || item == "zip" => "zip",
            var item when item.EndsWith(".7z", StringComparison.Ordinal) || item == "7z" => "7z",
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
            _ => "default",
        };
    }

    private static string BuildCommand(string executablePath, string arguments)
    {
        return $"\"{executablePath}\" {arguments}";
    }
}
