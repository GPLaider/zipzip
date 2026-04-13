using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ZipZip.ArchiveAdapters.SevenZip;
using ZipZip.Domain.Models;

namespace ZipZip.ShellExtension;

internal sealed partial class SevenZipShellExtractRunner
{
    private readonly SevenZipBackendOptions _options;

    public SevenZipShellExtractRunner(SevenZipBackendOptions options)
    {
        _options = options;
    }

    public async Task<string> ExtractAsync(
        string archivePath,
        ExtractionOptions options,
        IProgress<ShellExtractProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var destinationPath = SevenZipArchiveBackend.ResolveExtractionDestinationPath(archivePath, options);
        var arguments = new List<string>
        {
            "x",
            archivePath,
            $"-o{destinationPath}",
            options.OverwriteExisting ? "-y" : "-aos",
            "-bb1",
            "-bsp1",
        };

        if (!string.IsNullOrWhiteSpace(options.Password))
        {
            arguments.Add($"-p{options.Password}");
        }

        if (options.SelectedEntries is { Count: > 0 })
        {
            arguments.AddRange(options.SelectedEntries);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var registration = cancellationToken.Register(() => TryKill(process));

        var standardError = new StringBuilder();
        var stdoutTask = ReadProgressStreamAsync(process.StandardOutput, progress, cancellationToken);
        var stderrTask = ReadErrorStreamAsync(process.StandardError, standardError, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask);

        if (process.ExitCode != 0)
        {
            var details = standardError.ToString().Trim();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(details)
                ? "\uC555\uCD95 \uD480\uAE30 \uC911 \uC624\uB958\uAC00 \uBC1C\uC0DD\uD588\uC2B5\uB2C8\uB2E4."
                : details);
        }

        progress?.Report(new ShellExtractProgressUpdate(
            Percent: 100,
            StatusText: "\uC555\uCD95 \uD480\uAE30\uC5D0 \uC131\uACF5\uD558\uC600\uC2B5\uB2C8\uB2E4."));

        return destinationPath;
    }

    private static async Task ReadProgressStreamAsync(
        StreamReader reader,
        IProgress<ShellExtractProgressUpdate>? progress,
        CancellationToken cancellationToken)
    {
        var segment = new StringBuilder();
        var buffer = new char[1];

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
            if (read == 0)
            {
                break;
            }

            var character = buffer[0];
            if (character is '\r' or '\n')
            {
                FlushSegment(segment, progress);
                continue;
            }

            segment.Append(character);
        }

        FlushSegment(segment, progress);
    }

    private static async Task ReadErrorStreamAsync(
        StreamReader reader,
        StringBuilder target,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                target.AppendLine(line);
            }
        }
    }

    private static void FlushSegment(StringBuilder segment, IProgress<ShellExtractProgressUpdate>? progress)
    {
        if (segment.Length == 0)
        {
            return;
        }

        var text = segment.ToString();
        segment.Clear();

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var trimmed = text.Trim();
        int? percent = null;
        string? currentFile = null;
        string? statusText = null;

        var percentMatch = ProgressRegex().Match(trimmed);
        if (percentMatch.Success && int.TryParse(percentMatch.Groups["percent"].Value, out var parsedPercent))
        {
            percent = Math.Clamp(parsedPercent, 0, 100);
        }

        if (trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            currentFile = trimmed[2..].Trim();
        }
        else if (trimmed.StartsWith("Extracting archive:", StringComparison.OrdinalIgnoreCase))
        {
            statusText = "\uC555\uCD95 \uD480\uAE30\uB97C \uC2DC\uC791\uD569\uB2C8\uB2E4.";
        }
        else if (trimmed.StartsWith("Everything is Ok", StringComparison.OrdinalIgnoreCase))
        {
            statusText = "\uB9C8\uBB34\uB9AC\uD558\uB294 \uC911...";
        }

        if (percent is null && currentFile is null && statusText is null)
        {
            return;
        }

        progress?.Report(new ShellExtractProgressUpdate(percent, currentFile, statusText));
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    [GeneratedRegex(@"^(?<percent>\d{1,3})%")]
    private static partial Regex ProgressRegex();
}

internal sealed record ShellExtractProgressUpdate(
    int? Percent = null,
    string? CurrentFile = null,
    string? StatusText = null);
