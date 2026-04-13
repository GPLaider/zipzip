using System.Diagnostics;

namespace ZipZip.App.Services;

public sealed class ExplorerMenuService
{
    public async Task ApplyAsync(ShellMenuPreferences preferences, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var appExecutablePath = Environment.ProcessPath
                                ?? throw new InvalidOperationException("\uC2E4\uD589 \uC911\uC778 ZipZip \uACBD\uB85C\uB97C \uD655\uC778\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");

        if (!File.Exists(appExecutablePath))
        {
            throw new FileNotFoundException("\uC2E4\uD589 \uD30C\uC77C\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", appExecutablePath);
        }

        var shellHelperPath = ResolveShellHelperPath(appExecutablePath);
        if (!File.Exists(shellHelperPath))
        {
            throw new FileNotFoundException("\uC178 \uB3C4\uC6B0\uBBF8 \uD30C\uC77C\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", shellHelperPath);
        }

        var arguments = string.Join(
            ' ',
            [
                "register",
                Quote(appExecutablePath),
                $"--show-open={ToFlag(preferences.ShowOpenWithZipZip)}",
                $"--show-extract-here={ToFlag(preferences.ShowExtractHere)}",
                $"--show-extract-new-folder={ToFlag(preferences.ShowExtractNewFolder)}",
                $"--show-compress-dialog={ToFlag(preferences.ShowCompressDialog)}",
                $"--show-compress-zip={ToFlag(preferences.ShowCompressZip)}",
                $"--show-compress-7z={ToFlag(preferences.ShowCompressSevenZip)}",
            ]);

        var startInfo = new ProcessStartInfo
        {
            FileName = shellHelperPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("\uD0D0\uC0C9\uAE30 \uBA54\uB274 \uB3C4\uC6B0\uBBF8\uB97C \uC2E4\uD589\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.");

        await process.WaitForExitAsync(cancellationToken);
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
            return;
        }

        var message = string.IsNullOrWhiteSpace(error) ? output : error;
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(message)
                ? "\uD0D0\uC0C9\uAE30 \uBA54\uB274 \uC124\uC815\uC744 \uC801\uC6A9\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4."
                : message.Trim());
    }

    private static string ResolveShellHelperPath(string appExecutablePath)
    {
        var appDirectory = Path.GetDirectoryName(appExecutablePath)
                           ?? throw new InvalidOperationException("\uC571 \uD3F4\uB354 \uACBD\uB85C\uB97C \uD655\uC778\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");

        var localCandidate = Path.Combine(appDirectory, "ZipZip.ShellExtension.exe");
        if (File.Exists(localCandidate))
        {
            return localCandidate;
        }

        var targetFramework = new DirectoryInfo(appDirectory).Name;
        var configurationDirectory = Directory.GetParent(appDirectory);
        var platformDirectory = configurationDirectory?.Parent;
        var projectDirectory = platformDirectory?.Parent?.Parent;
        var srcDirectory = projectDirectory?.Parent;

        if (configurationDirectory is not null && platformDirectory is not null && srcDirectory is not null)
        {
            var siblingCandidate = Path.Combine(
                srcDirectory.FullName,
                "ZipZip.ShellExtension",
                "bin",
                platformDirectory.Name,
                configurationDirectory.Name,
                targetFramework,
                "ZipZip.ShellExtension.exe");

            if (File.Exists(siblingCandidate))
            {
                return siblingCandidate;
            }
        }

        throw new FileNotFoundException("\uC178 \uB3C4\uC6B0\uBBF8 \uD30C\uC77C\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
    }

    private static string Quote(string value) => $"\"{value}\"";

    private static int ToFlag(bool value) => value ? 1 : 0;
}
