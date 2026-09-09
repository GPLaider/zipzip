using ZipZip.Domain.Models;

namespace ZipZip.ArchiveAdapters.SevenZip;

public static class SevenZipOutputParser
{
    public static IReadOnlyList<ArchiveEntry> ParseListOutput(string output)
    {
        var entries = new List<ArchiveEntry>();
        var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in output.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine;

            if (string.IsNullOrWhiteSpace(line))
            {
                TryAppendEntry(current, entries);
                current.Clear();
                continue;
            }

            var separatorIndex = line.IndexOf(" = ", StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 3)..];
            current[key] = value;
        }

        TryAppendEntry(current, entries);

        // ZIP writers may store only files, without separate records for parent folders.
        var paths = entries.Select(entry => (entry.Path ?? entry.Name).TrimEnd('/')).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in entries.ToArray())
        {
            var parent = (entry.Path ?? entry.Name).TrimEnd('/');
            while (parent.LastIndexOf('/') is var index && index > 0)
            {
                parent = parent[..index];
                if (paths.Add(parent))
                    entries.Add(new ArchiveEntry { Path = parent, Name = parent[(parent.LastIndexOf('/') + 1)..], IsDirectory = true, TypeLabel = "폴더" });
            }
        }
        return entries;
    }

    private static void TryAppendEntry(IReadOnlyDictionary<string, string> values, ICollection<ArchiveEntry> entries)
    {
        if (!values.TryGetValue("Path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (values.ContainsKey("Physical Size") || values.ContainsKey("Headers Size"))
        {
            return;
        }

        var attributes = values.TryGetValue("Attributes", out var rawAttributes)
            ? rawAttributes
            : string.Empty;
        path = path.Replace('\\', '/');
        var isDirectory = (values.TryGetValue("Folder", out var folder) && folder == "+")
            || attributes.Contains('D', StringComparison.OrdinalIgnoreCase) || path.EndsWith('/');

        entries.Add(new ArchiveEntry
        {
            Name = Path.GetFileName(path.TrimEnd('/', '\\')),
            Path = path,
            TypeLabel = DetectTypeLabel(path, isDirectory),
            PackedSize = ParseLong(values, "Packed Size"),
            OriginalSize = ParseLong(values, "Size"),
            IsDirectory = isDirectory,
        });
    }

    private static long ParseLong(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var raw) && long.TryParse(raw, out var number)
            ? number
            : 0L;
    }

    private static string DetectTypeLabel(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            return "폴더";
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" => "텍스트 문서",
            ".png" or ".jpg" or ".jpeg" or ".gif" => "이미지 파일",
            ".exe" => "실행 파일",
            ".dll" => "라이브러리 파일",
            ".pdf" => "PDF 문서",
            _ when string.IsNullOrEmpty(extension) => "파일",
            _ => $"{extension.TrimStart('.').ToUpperInvariant()} 파일",
        };
    }
}
