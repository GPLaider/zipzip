using System.Diagnostics;
using System.Runtime.InteropServices;
using ZipZip.App.Visuals;
using Microsoft.Win32;

namespace ZipZip.App.Services;

public sealed class FileAssociationService
{
    private const string RegisteredApplicationsKeyPath = @"Software\RegisteredApplications";
    private const string CapabilitiesKeyPath = @"Software\ZipZip\Capabilities";
    private const string CapabilitiesFileAssociationsKeyPath = @"Software\ZipZip\Capabilities\FileAssociations";
    private const string ApplicationRegistrationKeyPath = @"Software\Classes\Applications\ZipZip.App.exe";

    public Task ApplyAsync(IReadOnlyList<string> selectedExtensions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var appExecutablePath = Environment.ProcessPath
                                ?? throw new InvalidOperationException("\uC2E4\uD589 \uC911\uC778 ZipZip \uACBD\uB85C\uB97C \uD655\uC778\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");

        if (!File.Exists(appExecutablePath))
        {
            throw new FileNotFoundException("\uC2E4\uD589 \uD30C\uC77C\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", appExecutablePath);
        }

        var selectedSet = NormalizeSelectedExtensions(selectedExtensions);
        RegisterApplicationCapabilities(appExecutablePath, selectedSet);

        foreach (var extension in FileAssociationCatalog.AllExtensions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RegisterOrRemoveAssociation(appExecutablePath, extension, selectedSet.Contains(extension));
        }

        NotifyShellAssociationsChanged();
        return Task.CompletedTask;
    }

    public void OpenDefaultAppsSettings()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:defaultapps",
            UseShellExecute = true,
        });
    }

    private static HashSet<string> NormalizeSelectedExtensions(IReadOnlyList<string> selectedExtensions)
    {
        var supported = new HashSet<string>(FileAssociationCatalog.AllExtensions, StringComparer.OrdinalIgnoreCase);
        return selectedExtensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(extension => extension.StartsWith('.') ? extension : $".{extension}")
            .Where(extension => supported.Contains(extension))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void RegisterApplicationCapabilities(string appExecutablePath, HashSet<string> selectedSet)
    {
        using var registeredApplications = Registry.CurrentUser.CreateSubKey(RegisteredApplicationsKeyPath);
        registeredApplications?.SetValue("ZipZip", @"Software\ZipZip\Capabilities");

        using var capabilities = Registry.CurrentUser.CreateSubKey(CapabilitiesKeyPath);
        if (capabilities is null)
        {
            throw new InvalidOperationException("\uD30C\uC77C \uC5F0\uACB0 \uC124\uC815\uC744 \uC800\uC7A5\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
        }

        capabilities.SetValue("ApplicationName", "ZipZip");
        capabilities.SetValue(
            "ApplicationDescription",
            "\uD55C\uAD6D \uC0AC\uC6A9\uC790\uC5D0 \uB9DE\uCD98 Windows \uC555\uCD95 \uD30C\uC77C \uAD00\uB9AC \uB3C4\uAD6C");

        Registry.CurrentUser.DeleteSubKeyTree(CapabilitiesFileAssociationsKeyPath, throwOnMissingSubKey: false);
        using var fileAssociations = Registry.CurrentUser.CreateSubKey(CapabilitiesFileAssociationsKeyPath);
        foreach (var extension in selectedSet)
        {
            fileAssociations?.SetValue(extension, BuildProgId(extension));
        }

        Registry.CurrentUser.DeleteSubKeyTree($@"{ApplicationRegistrationKeyPath}\SupportedTypes", throwOnMissingSubKey: false);
        using var applicationKey = Registry.CurrentUser.CreateSubKey(ApplicationRegistrationKeyPath);
        applicationKey?.SetValue("FriendlyAppName", "ZipZip");

        using var supportedTypes = Registry.CurrentUser.CreateSubKey($@"{ApplicationRegistrationKeyPath}\SupportedTypes");
        foreach (var extension in selectedSet)
        {
            supportedTypes?.SetValue(extension, string.Empty);
        }

        using var commandKey = Registry.CurrentUser.CreateSubKey($@"{ApplicationRegistrationKeyPath}\shell\open\command");
        commandKey?.SetValue(string.Empty, BuildOpenCommand(appExecutablePath));
    }

    private static void RegisterOrRemoveAssociation(string appExecutablePath, string extension, bool isSelected)
    {
        var progId = BuildProgId(extension);

        if (isSelected)
        {
            RegisterProgId(appExecutablePath, extension, progId);
            RegisterExtensionMapping(extension, progId);
            return;
        }

        RemoveExtensionMapping(extension, progId);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{progId}", throwOnMissingSubKey: false);
    }

    private static void RegisterProgId(string appExecutablePath, string extension, string progId)
    {
        using var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
        if (progIdKey is null)
        {
            throw new InvalidOperationException("\uD30C\uC77C \uC5F0\uACB0 \uD504\uB85C\uADF8\uB7A8 ID\uB97C \uB4F1\uB85D\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
        }

        var label = FileAssociationCatalog.GetLabel(extension);
        progIdKey.SetValue(string.Empty, $"ZipZip {label} \uD30C\uC77C");
        progIdKey.SetValue("FriendlyTypeName", $"ZipZip {label} \uD30C\uC77C");

        using var iconKey = progIdKey.CreateSubKey("DefaultIcon");
        iconKey?.SetValue(string.Empty, ArchiveFormatAssetCatalog.GetShellIconPath(appExecutablePath, extension));

        using var commandKey = progIdKey.CreateSubKey(@"shell\open\command");
        commandKey?.SetValue(string.Empty, BuildOpenCommand(appExecutablePath));
    }

    private static void RegisterExtensionMapping(string extension, string progId)
    {
        using var extensionKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}");
        using var openWithProgIds = extensionKey?.CreateSubKey("OpenWithProgids");

        openWithProgIds?.SetValue(progId, string.Empty);

        if (!HasExplicitUserChoice(extension))
        {
            extensionKey?.SetValue(string.Empty, progId);
            return;
        }

        var currentDefault = extensionKey?.GetValue(string.Empty) as string;
        if (string.Equals(currentDefault, progId, StringComparison.OrdinalIgnoreCase))
        {
            extensionKey?.SetValue(string.Empty, progId);
        }
    }

    private static void RemoveExtensionMapping(string extension, string progId)
    {
        using var extensionKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}");
        using var openWithProgIds = extensionKey?.CreateSubKey("OpenWithProgids");

        openWithProgIds?.DeleteValue(progId, throwOnMissingValue: false);

        var currentDefault = extensionKey?.GetValue(string.Empty) as string;
        if (!HasExplicitUserChoice(extension)
            && string.Equals(currentDefault, progId, StringComparison.OrdinalIgnoreCase))
        {
            extensionKey?.DeleteValue(string.Empty, throwOnMissingValue: false);
        }
    }

    private static bool HasExplicitUserChoice(string extension)
    {
        using var userChoice = Registry.CurrentUser.OpenSubKey(
            $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\UserChoice");

        return userChoice?.GetValue("ProgId") is string progId
               && !string.IsNullOrWhiteSpace(progId);
    }

    private static string BuildProgId(string extension)
    {
        return $"ZipZip.Assoc.{extension.TrimStart('.').ToUpperInvariant()}";
    }

    private static string BuildOpenCommand(string appExecutablePath)
    {
        return $"\"{appExecutablePath}\" \"%1\"";
    }

    private static void NotifyShellAssociationsChanged()
    {
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
