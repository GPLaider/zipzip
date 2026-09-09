using ZipZip.Domain.Models;

namespace ZipZip.Application;

public static class ArchiveEntryQuery
{
    public static IReadOnlyList<ArchiveEntry> Apply(IEnumerable<ArchiveEntry> entries, string folder, string query, int sortIndex)
    {
        var normalizedFolder = folder.Replace('\\', '/').Trim('/');
        var search = query.Trim();
        var visible = entries.Where(entry =>
        {
            var path = (entry.Path ?? entry.Name).Replace('\\', '/').Trim('/');
            var slash = path.LastIndexOf('/');
            var parent = slash < 0 ? string.Empty : path[..slash];
            return parent == normalizedFolder && entry.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
        }).OrderByDescending(entry => entry.IsDirectory);

        return (sortIndex switch
        {
            1 => visible.ThenByDescending(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
            2 => visible.ThenByDescending(entry => entry.OriginalSize).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
            3 => visible.ThenBy(entry => entry.OriginalSize).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
            _ => visible.ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
        }).ToArray();
    }
}
