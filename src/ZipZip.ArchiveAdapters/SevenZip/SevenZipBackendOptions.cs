namespace ZipZip.ArchiveAdapters.SevenZip;

public sealed record SevenZipBackendOptions(string ExecutablePath)
{
    public static SevenZipBackendOptions Default { get; } = new(ResolveExecutablePath());

    private static string ResolveExecutablePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "7z.exe"),
            Environment.ProcessPath is null
                ? null
                : Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "7z.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "7z.exe"),
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        return "7z.exe";
    }
}
