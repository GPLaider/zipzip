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
            var line = rawLine.Trim();

            if (string.IsNullOrEmpty(line))
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
            var value = line[(separatorIndex + 3)..].Trim();
            current[key] = value;
        }

        TryAppendEntry(current, entries);
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

        entries.Add(new ArchiveEntry
        {
            Name = Path.GetFileName(path.TrimEnd('/', '\\')),
            Path = path,
            TypeLabel = DetectTypeLabel(path, attributes),
            PackedSize = ParseLong(values, "Packed Size"),
            OriginalSize = ParseLong(values, "Size"),
            IsDirectory = attributes.Contains('D', StringComparison.OrdinalIgnoreCase),
        });
    }

    private static long ParseLong(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var raw) && long.TryParse(raw, out var number)
            ? number
            : 0L;
    }

    private static string DetectTypeLabel(string path, string attributes)
    {
        if (attributes.Contains('D', StringComparison.OrdinalIgnoreCase))
        {
            return "?대뜑";
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" => "?띿뒪??臾몄꽌",
            ".png" or ".jpg" or ".jpeg" or ".gif" => "?대?吏 ?뚯씪",
            ".exe" => "?ㅽ뻾 ?뚯씪",
            ".dll" => "?쇱씠釉뚮윭由??뚯씪",
            ".pdf" => "PDF 臾몄꽌",
            _ when string.IsNullOrEmpty(extension) => "?뚯씪",
            _ => $"{extension.TrimStart('.').ToUpperInvariant()} ?뚯씪",
        };
    }
}
