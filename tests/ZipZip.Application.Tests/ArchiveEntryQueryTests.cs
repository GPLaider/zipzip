using ZipZip.Domain.Models;

namespace ZipZip.Application.Tests;

public class ArchiveEntryQueryTests
{
    [Fact]
    public void FiltersDirectChildrenLiterallyAndSortsNumericSizesWithFoldersFirst()
    {
        ArchiveEntry[] entries = [
            new() { Name = "folder", Path = "root/folder", IsDirectory = true },
            new() { Name = "B.TXT", Path = "root/B.TXT", OriginalSize = 100 },
            new() { Name = "a.txt", Path = "root\\a.txt", OriginalSize = 9 },
            new() { Name = "문서[1].txt", Path = "root/문서[1].txt", OriginalSize = 2 },
            new() { Name = "hidden.txt", Path = "root/folder/hidden.txt", OriginalSize = 999 },
            new() { Name = "outside.txt", Path = "outside.txt", OriginalSize = 999 },
        ];
        Assert.Equal(new[] { "folder", "B.TXT", "a.txt", "문서[1].txt" }, ArchiveEntryQuery.Apply(entries, "root", "", 2).Select(e => e.Name));
        Assert.Equal(new[] { "folder", "문서[1].txt", "a.txt", "B.TXT" }, ArchiveEntryQuery.Apply(entries, "root", "", 3).Select(e => e.Name));
        Assert.Equal(3, ArchiveEntryQuery.Apply(entries, "root\\", " .TxT ", 0).Count);
        Assert.Equal("문서[1].txt", Assert.Single(ArchiveEntryQuery.Apply(entries, "root", "[1]", 0)).Name);
        Assert.Empty(ArchiveEntryQuery.Apply(entries, "root", "*", 0));
        Assert.Equal("hidden.txt", Assert.Single(ArchiveEntryQuery.Apply(entries, "root/folder", "", 0)).Name);
        var ascending = ArchiveEntryQuery.Apply(entries, "root", ".txt", 0);
        Assert.Equal(ascending.Reverse(), ArchiveEntryQuery.Apply(entries, "root", ".txt", 1));
    }
}
