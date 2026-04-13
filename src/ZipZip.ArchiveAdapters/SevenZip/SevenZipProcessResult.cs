namespace ZipZip.ArchiveAdapters.SevenZip;

internal sealed record SevenZipProcessResult(int ExitCode, string StandardOutput, string StandardError);
