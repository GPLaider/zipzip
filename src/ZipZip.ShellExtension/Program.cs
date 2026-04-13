using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZipZip.Application.UseCases;
using ZipZip.ArchiveAdapters.SevenZip;
using ZipZip.Domain.Models;

namespace ZipZip.ShellExtension;

public static class Program
{
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowError("ZipZip 셸 도우미", "실행할 작업이 지정되지 않았습니다.");
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
                        Environment.ProcessPath ?? throw new InvalidOperationException("셸 도우미 경로를 확인할 수 없습니다."),
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
                    ShowError("ZipZip 셸 도우미", $"지원하지 않는 작업입니다: {args[0]}");
                    return 1;
            }
        }
        catch (Exception ex) when (NeedsPassword(ex))
        {
            ShowError("ZipZip 작업 실패", "암호가 필요한 압축 파일입니다. 다시 시도해서 암호를 입력해 주세요.");
            return 1;
        }
        catch (Exception ex)
        {
            ShowError("ZipZip 작업 실패", ex.Message);
            return 1;
        }
    }

    private static string ResolveAppExecutablePath(string[] args)
    {
        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
        {
            return args[1];
        }

        throw new InvalidOperationException("ZipZip 앱 경로가 필요합니다.");
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
            throw new InvalidOperationException("압축 파일 경로를 확인할 수 없습니다.");
        }

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        var requestedOptions = new ExtractionOptions(
            DestinationPath: parentDirectory,
            CreateNewFolder: createNewFolder,
            OverwriteExisting: false);
        var options = PrepareExtractionOptions(archivePath, requestedOptions);

        using var progressForm = new ShellExtractProgressForm(archivePath);
        using var cancellationSource = new CancellationTokenSource();
        var runner = new SevenZipShellExtractRunner(SevenZipBackendOptions.Default);

        progressForm.CancelRequested += (_, _) => cancellationSource.Cancel();

        progressForm.Shown += async (_, _) =>
        {
            var currentOptions = options;
            var promptMessage = "암호가 필요한 압축 파일입니다. 암호를 입력해 주세요.";

            try
            {
                while (true)
                {
                    try
                    {
                        var progress = new Progress<ShellExtractProgressUpdate>(progressForm.ApplyProgress);
                        var completedFolderPath = await runner.ExtractAsync(
                            archivePath,
                            currentOptions,
                            progress,
                            cancellationSource.Token);

                        progressForm.CompleteSuccess(completedFolderPath);
                        return;
                    }
                    catch (Exception ex) when (NeedsPassword(ex))
                    {
                        var password = PasswordPromptForm.ShowDialog(progressForm, promptMessage);
                        if (password is null)
                        {
                            progressForm.CompleteCanceled();
                            return;
                        }

                        currentOptions = currentOptions with { Password = password };
                        promptMessage = "암호가 올바르지 않습니다. 다시 입력해 주세요.";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                progressForm.CompleteCanceled();
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
            throw new InvalidOperationException("대상 파일이나 폴더가 필요합니다.");
        }

        return inputPaths;
    }

    private static string RequireSinglePath(string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
        {
            throw new InvalidOperationException("대상 파일 경로가 필요합니다.");
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
            throw new InvalidOperationException("출력 폴더를 찾을 수 없습니다.");
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

    private static ExtractionOptions PrepareExtractionOptions(string archivePath, ExtractionOptions options)
    {
        var destinationPath = SevenZipArchiveBackend.ResolveExtractionDestinationPath(archivePath, options);
        return options with
        {
            DestinationPath = destinationPath,
            CreateNewFolder = false,
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
