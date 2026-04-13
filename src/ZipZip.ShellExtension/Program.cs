using System.Runtime.InteropServices;
using ZipZip.Application.UseCases;
using ZipZip.ArchiveAdapters.SevenZip;
using ZipZip.Domain.Models;
using System.Windows.Forms;

namespace ZipZip.ShellExtension;

public static class Program
{
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowError("ZipZip \uC178 \uB3C4\uC6B0\uBBF8", "\uC2E4\uD589\uD560 \uC791\uC5C5\uC774 \uC9C0\uC815\uB418\uC9C0 \uC54A\uC558\uC2B5\uB2C8\uB2E4.");
            return 1;
        }

        try
        {
            var backend = new SevenZipArchiveBackend();
            var createArchive = new CreateArchiveUseCase(backend);
            var registration = new ShellRegistrationService();

            switch (args[0].ToLowerInvariant())
            {
                case "register":
                    registration.Register(
                        ResolveAppExecutablePath(args),
                        Environment.ProcessPath ?? throw new InvalidOperationException("\uC178 \uB3C4\uC6B0\uBBF8 \uACBD\uB85C\uB97C \uD655\uC778\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4."),
                        ParseShellMenuOptions(args.Skip(2).ToArray()));
                    return 0;

                case "unregister":
                    registration.Unregister();
                    return 0;

                case "extract-here":
                    return await ExtractWithProgressAsync(RequireSinglePath(args), createNewFolder: false);

                case "extract-new-folder":
                    return await ExtractWithProgressAsync(RequireSinglePath(args), createNewFolder: true);

                case "compress-zip":
                    await CompressAsync(createArchive, RequireInputPaths(args), ArchiveFormat.Zip);
                    return 0;

                case "compress-7z":
                    await CompressAsync(createArchive, RequireInputPaths(args), ArchiveFormat.SevenZip);
                    return 0;

                default:
                    ShowError("ZipZip \uC178 \uB3C4\uC6B0\uBBF8", $"\uC9C0\uC6D0\uD558\uC9C0 \uC54A\uB294 \uC791\uC5C5\uC785\uB2C8\uB2E4: {args[0]}");
                    return 1;
            }
        }
        catch (Exception ex) when (NeedsPassword(ex))
        {
            ShowError("ZipZip \uC791\uC5C5 \uC2E4\uD328", "\uC554\uD638\uAC00 \uD544\uC694\uD55C \uC555\uCD95 \uD30C\uC77C\uC785\uB2C8\uB2E4. ZipZip\uC5D0\uC11C \uD30C\uC77C\uC744 \uC5F4 \uB4A4 \uC554\uD638\uB97C \uC785\uB825\uD574 \uC8FC\uC138\uC694.");
            return 1;
        }
        catch (Exception ex)
        {
            ShowError("ZipZip \uC791\uC5C5 \uC2E4\uD328", ex.Message);
            return 1;
        }
    }

    private static string ResolveAppExecutablePath(string[] args)
    {
        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
        {
            return args[1];
        }

        throw new InvalidOperationException("ZipZip \uC571 \uACBD\uB85C\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4.");
    }

    private static ShellRegistrationService.ShellMenuRegistrationOptions ParseShellMenuOptions(IReadOnlyList<string> args)
    {
        var options = ShellRegistrationService.ShellMenuRegistrationOptions.Default;

        foreach (var argument in args)
        {
            if (string.IsNullOrWhiteSpace(argument) || !argument.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = argument.Split('=', 2, StringSplitOptions.TrimEntries);
            var value = parts.Length == 2 && !string.Equals(parts[1], "0", StringComparison.OrdinalIgnoreCase);

            switch (parts[0].ToLowerInvariant())
            {
                case "--show-open":
                    options = options with { ShowOpenWithZipZip = value };
                    break;
                case "--show-extract-here":
                    options = options with { ShowExtractHere = value };
                    break;
                case "--show-extract-new-folder":
                    options = options with { ShowExtractNewFolder = value };
                    break;
                case "--show-compress-dialog":
                    options = options with { ShowCompressDialog = value };
                    break;
                case "--show-compress-zip":
                    options = options with { ShowCompressZip = value };
                    break;
                case "--show-compress-7z":
                    options = options with { ShowCompressSevenZip = value };
                    break;
            }
        }

        return options;
    }

    private static async Task<int> ExtractWithProgressAsync(string archivePath, bool createNewFolder)
    {
        var parentDirectory = Path.GetDirectoryName(archivePath);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            throw new InvalidOperationException("\uC555\uCD95 \uD30C\uC77C \uACBD\uB85C\uB97C \uD655\uC778\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
        }

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        var options = new ExtractionOptions(
            DestinationPath: parentDirectory,
            CreateNewFolder: createNewFolder,
            OverwriteExisting: false);

        using var progressForm = new ShellExtractProgressForm(archivePath);
        using var cancellationSource = new CancellationTokenSource();
        var runner = new SevenZipShellExtractRunner(SevenZipBackendOptions.Default);

        progressForm.CancelRequested += (_, _) => cancellationSource.Cancel();

        progressForm.Shown += async (_, _) =>
        {
            try
            {
                var progress = new Progress<ShellExtractProgressUpdate>(progressForm.ApplyProgress);
                var completedFolderPath = await runner.ExtractAsync(
                    archivePath,
                    options,
                    progress,
                    cancellationSource.Token);

                progressForm.CompleteSuccess(completedFolderPath);
            }
            catch (OperationCanceledException)
            {
                progressForm.CompleteCanceled();
            }
            catch (Exception ex) when (NeedsPassword(ex))
            {
                progressForm.CompleteFailure(
                    "\uC554\uD638\uAC00 \uD544\uC694\uD55C \uC555\uCD95 \uD30C\uC77C\uC785\uB2C8\uB2E4. ZipZip\uC5D0\uC11C \uD30C\uC77C\uC744 \uC5F4 \uB4A4 \uC554\uD638\uB97C \uC785\uB825\uD574 \uC8FC\uC138\uC694.");
            }
            catch (Exception ex)
            {
                progressForm.CompleteFailure(ex.Message);
            }
        };
        System.Windows.Forms.Application.Run(progressForm);

        return progressForm.ExitCode;
    }

    private static async Task CompressAsync(
        CreateArchiveUseCase createArchive,
        IReadOnlyList<string> inputPaths,
        ArchiveFormat format)
    {
        var outputPath = BuildOutputPath(inputPaths, format);
        await createArchive.ExecuteAsync(
            inputPaths,
            new CompressionOptions(
                OutputPath: outputPath,
                Format: format,
                Level: CompressionLevel.Normal));
    }

    private static IReadOnlyList<string> RequireInputPaths(string[] args)
    {
        var inputPaths = args.Skip(1).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (inputPaths.Length == 0)
        {
            throw new InvalidOperationException("\uB300\uC0C1 \uD30C\uC77C\uC774\uB098 \uD3F4\uB354\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4.");
        }

        return inputPaths;
    }

    private static string RequireSinglePath(string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
        {
            throw new InvalidOperationException("\uB300\uC0C1 \uD30C\uC77C \uACBD\uB85C\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4.");
        }

        return args[1];
    }

    private static string BuildOutputPath(IReadOnlyList<string> inputPaths, ArchiveFormat format)
    {
        var firstPath = inputPaths[0];
        var targetDirectory = ResolveTargetDirectory(firstPath);
        var baseName = ResolveBaseName(firstPath, inputPaths.Count);
        var extension = format == ArchiveFormat.SevenZip ? ".7z" : ".zip";

        var candidate = Path.Combine(targetDirectory, baseName + extension);
        var number = 2;

        while (File.Exists(candidate) || Directory.Exists(candidate))
        {
            candidate = Path.Combine(targetDirectory, $"{baseName} ({number++}){extension}");
        }

        return candidate;
    }

    private static string ResolveTargetDirectory(string inputPath)
    {
        if (Directory.Exists(inputPath))
        {
            var parent = Directory.GetParent(inputPath)?.FullName;
            if (!string.IsNullOrWhiteSpace(parent))
            {
                return parent;
            }
        }

        var directory = Path.GetDirectoryName(inputPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("\uCD9C\uB825 \uD3F4\uB354\uB97C \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
        }

        return directory;
    }

    private static string ResolveBaseName(string inputPath, int inputCount)
    {
        if (inputCount > 1)
        {
            var parentDirectory = ResolveTargetDirectory(inputPath);
            return Path.GetFileName(parentDirectory);
        }

        return Path.GetFileName(inputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static void ShowError(string title, string message)
    {
        MessageBoxW(IntPtr.Zero, message, title, 0x00000010);
    }

    private static bool NeedsPassword(Exception exception)
    {
        return SevenZipErrorClassifier.RequiresPassword(exception.Message);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
