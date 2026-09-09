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
        CancellationToken cancellationToken = default, IProgress<int>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("-sccUTF-8");
        if (progress is not null) startInfo.ArgumentList.Add("-bsp2");

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        process.StandardInput.Close(); // GUI operations must never wait for an invisible console prompt.

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = progress is null ? process.StandardError.ReadToEndAsync() : ReadProgressAsync(process.StandardError, progress);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { } // Process already exited.
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdoutTask, stderrTask);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new SevenZipProcessResult(process.ExitCode, stdout, stderr);
    }

    private static async Task<string> ReadProgressAsync(StreamReader reader, IProgress<int> progress)
    {
        var errors = new StringBuilder();
        var segment = new StringBuilder();
        var buffer = new char[1024];
        void Flush()
        {
            var text = segment.ToString().Trim();
            segment.Clear();
            if (text.Length == 0) return;
            var percent = text.IndexOf('%');
            if (percent > 0 && int.TryParse(text.AsSpan(0, percent), out var value) && value is >= 0 and <= 100)
                progress.Report(value);
            else errors.AppendLine(text);
        }
        int count;
        while ((count = await reader.ReadAsync(buffer)) != 0)
            for (var i = 0; i < count; i++)
                if (buffer[i] is '\r' or '\n' or '\b') Flush();
                else segment.Append(buffer[i]);
        Flush();
        return errors.ToString();
    }
}
