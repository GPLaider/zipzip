using System.Diagnostics;
using System.Text;

namespace ZipZip.ArchiveAdapters.SevenZip;

internal sealed class SevenZipProcessRunner
{
    private readonly SevenZipBackendOptions _options;

    public SevenZipProcessRunner(SevenZipBackendOptions options)
    {
        _options = options;
    }

    public async Task<SevenZipProcessResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new SevenZipProcessResult(process.ExitCode, stdout, stderr);
    }
}
