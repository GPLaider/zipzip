using ZipZip.Domain.Models;
using ZipZip.Domain.Services;

namespace ZipZip.ArchiveAdapters.SevenZip;

public sealed class SevenZipArchiveBackend : IArchiveBackend
{
    private readonly SevenZipProcessRunner _runner;

    public SevenZipArchiveBackend()
        : this(SevenZipBackendOptions.Default)
    {
    }

    public SevenZipArchiveBackend(SevenZipBackendOptions options)
    {
        _runner = new SevenZipProcessRunner(options);
    }

    public async Task<ArchiveSummary> OpenAsync(string archivePath, CancellationToken cancellationToken = default, string? password = null)
    {
        var result = await ListAsync(archivePath, password, cancellationToken);
        EnsureSuccess(result, "압축 파일을 열지 못했습니다.");

        var entries = SevenZipOutputParser.ParseListOutput(result.StandardOutput);

        return new ArchiveSummary(
            archivePath,
            DetectFormat(archivePath),
            entries);
    }

    public async Task ExtractAsync(
        string archivePath,
        ExtractionOptions options,
        CancellationToken cancellationToken = default, IProgress<int>? progress = null)
    {
        await ValidateEncryptedArchiveAsync(archivePath, options.Password, cancellationToken);
        var destinationPath = ResolveExtractionDestinationPath(archivePath, options);

        var arguments = new List<string>
        {
            "x",
            $"-o{destinationPath}",
            options.OverwriteExisting ? "-y" : "-aos",
            "-spd",
        };

        if (!string.IsNullOrWhiteSpace(options.Password))
        {
            arguments.Add($"-p{options.Password}");
        }

        if (options.SelectedEntries is { Count: > 0 })
            arguments.AddRange(options.SelectedEntries.Select(entry => "-i!" + entry));
        arguments.Add("--");
        arguments.Add(Path.GetFullPath(archivePath));

        var result = await _runner.RunAsync(arguments, cancellationToken, progress);
        EnsureSuccess(result, "압축을 풀지 못했습니다.");
    }

    private Task<SevenZipProcessResult> ListAsync(string archivePath, string? password, CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "l", "-slt" };
        if (!string.IsNullOrEmpty(password)) arguments.Add("-p" + password);
        arguments.Add("--");
        arguments.Add(Path.GetFullPath(archivePath));
        return _runner.RunAsync(arguments, cancellationToken);
    }

    public async Task ValidateEncryptedArchiveAsync(string archivePath, string? password, CancellationToken cancellationToken = default)
    {
        var listing = await ListAsync(archivePath, password, cancellationToken);
        EnsureSuccess(listing, "압축 파일을 확인하지 못했습니다.");
        if (!listing.StandardOutput.Contains("Encrypted = +", StringComparison.Ordinal)) return;

        // Wrong passwords can leave zero-byte files that a later -aos retry would skip.
        // ponytail: encrypted archives are decoded twice; staged extraction if this becomes a bottleneck.
        var arguments = new List<string> { "t" };
        if (!string.IsNullOrEmpty(password)) arguments.Add("-p" + password);
        arguments.Add("--");
        arguments.Add(Path.GetFullPath(archivePath));
        EnsureSuccess(await _runner.RunAsync(arguments, cancellationToken), "압축 파일의 암호를 확인하지 못했습니다.");
    }

    public async Task CreateAsync(
        IReadOnlyList<string> inputPaths,
        CompressionOptions options,
        CancellationToken cancellationToken = default, IProgress<int>? progress = null)
    {
        if (File.Exists(options.OutputPath) || Directory.Exists(options.OutputPath) || File.Exists(options.OutputPath + ".001"))
            throw new IOException("같은 이름의 파일이 있습니다. 기존 파일을 보존하려면 다른 이름을 입력해 주세요.");
        if (options.EncryptFileNames && (options.Format != ArchiveFormat.SevenZip || string.IsNullOrEmpty(options.Password)))
            throw new ArgumentException("파일 이름 암호화에는 7Z 형식과 암호가 필요합니다.");

        var arguments = new List<string>
        {
            "a",
            FormatArgument(options.Format),
            CompressionLevelArgument(options.Level),
            "-spd",
        };

        if (!string.IsNullOrWhiteSpace(options.Password))
        {
            arguments.Add($"-p{options.Password}");
        }

        if (options.EncryptFileNames)
        {
            arguments.Add("-mhe=on");
        }

        if (!string.IsNullOrWhiteSpace(options.SplitSize))
        {
            arguments.Add($"-v{options.SplitSize}");
        }

        arguments.Add("--");
        arguments.Add(Path.GetFullPath(options.OutputPath));
        foreach (var inputPath in inputPaths)
        {
            arguments.Add(Path.GetFullPath(inputPath));
        }

        var result = await _runner.RunAsync(arguments, cancellationToken, progress);
        EnsureSuccess(result, "압축 파일을 만들지 못했습니다.");
    }

    public async Task TestAsync(string archivePath, CancellationToken cancellationToken = default, string? password = null, IProgress<int>? progress = null)
    {
        var arguments = new List<string> { "t" };
        if (!string.IsNullOrEmpty(password)) arguments.Add("-p" + password);
        arguments.Add("--");
        arguments.Add(Path.GetFullPath(archivePath));
        var result = await _runner.RunAsync(arguments, cancellationToken, progress);
        EnsureSuccess(result, "압축 파일 테스트에 실패했습니다.");
    }

    public static ArchiveFormat DetectFormat(string archivePath)
    {
        var fileName = Path.GetFileName(archivePath).ToLowerInvariant();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (fileName.EndsWith(".tar.gz", StringComparison.Ordinal) || fileName.EndsWith(".tgz", StringComparison.Ordinal))
        {
            return ArchiveFormat.GZip;
        }

        if (fileName.EndsWith(".tar.bz2", StringComparison.Ordinal)
            || fileName.EndsWith(".tbz", StringComparison.Ordinal)
            || fileName.EndsWith(".tbz2", StringComparison.Ordinal))
        {
            return ArchiveFormat.BZip2;
        }

        if (fileName.EndsWith(".tar.xz", StringComparison.Ordinal) || fileName.EndsWith(".txz", StringComparison.Ordinal))
        {
            return ArchiveFormat.Xz;
        }

        if (fileName.EndsWith(".tar.zst", StringComparison.Ordinal) || fileName.EndsWith(".tzst", StringComparison.Ordinal))
        {
            return ArchiveFormat.Zstd;
        }

        return extension switch
        {
            ".zip" => ArchiveFormat.Zip,
            ".7z" => ArchiveFormat.SevenZip,
            ".rar" => ArchiveFormat.Rar,
            ".tar" => ArchiveFormat.Tar,
            ".gz" => ArchiveFormat.GZip,
            ".bz2" => ArchiveFormat.BZip2,
            ".xz" => ArchiveFormat.Xz,
            ".zst" => ArchiveFormat.Zstd,
            ".lz" => ArchiveFormat.Unknown,
            ".lzma" => ArchiveFormat.Unknown,
            ".cab" => ArchiveFormat.Unknown,
            ".iso" => ArchiveFormat.Iso,
            ".wim" => ArchiveFormat.Unknown,
            ".arj" => ArchiveFormat.Unknown,
            ".cpio" => ArchiveFormat.Unknown,
            ".z" => ArchiveFormat.Unknown,
            ".lzh" => ArchiveFormat.Unknown,
            _ => ArchiveFormat.Unknown,
        };
    }

    private static void EnsureSuccess(SevenZipProcessResult result, string message)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        // 7-Zip writes the password prompt to stdout, but only "Break signaled" to stderr on EOF.
        if (result.StandardOutput.Contains("Enter password", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("암호가 필요한 압축 파일입니다.");

        var details = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;

        throw new InvalidOperationException($"{message}{Environment.NewLine}{details.Trim()}");
    }

    private static string FormatArgument(ArchiveFormat format)
    {
        return format switch
        {
            ArchiveFormat.Zip => "-tzip",
            ArchiveFormat.SevenZip => "-t7z",
            _ => "-tzip",
        };
    }

    private static string CompressionLevelArgument(CompressionLevel level)
    {
        return level switch
        {
            CompressionLevel.Fast => "-mx=3",
            CompressionLevel.Normal => "-mx=5",
            CompressionLevel.Maximum => "-mx=9",
            _ => "-mx=5",
        };
    }

    public static string ResolveExtractionDestinationPath(string archivePath, ExtractionOptions options)
    {
        if (!options.CreateNewFolder)
        {
            return options.DestinationPath;
        }

        var basePath = Path.Combine(options.DestinationPath, Path.GetFileNameWithoutExtension(archivePath));
        if (!File.Exists(basePath) && !Directory.Exists(basePath))
        {
            return basePath;
        }

        var number = 2;
        while (true)
        {
            var candidate = $"{basePath} ({number++})";
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
