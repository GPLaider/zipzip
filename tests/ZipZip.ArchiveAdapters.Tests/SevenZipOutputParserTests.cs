using ZipZip.ArchiveAdapters.SevenZip;
using ZipZip.Domain.Models;

namespace ZipZip.ArchiveAdapters.Tests;

public sealed class SevenZipOutputParserTests
{
    [Fact]
    public void ParseListOutput_Returns_Archive_Entries_Only()
    {
        const string output = """
Path = sample.zip
Type = zip
Physical Size = 512

----------
Path = docs
Size = 0
Packed Size = 0
Attributes = D_ drwxrwxrwx

Path = docs/readme.txt
Size = 2400
Packed Size = 1024
Attributes = A_ -rw-r--r--

Path = image.png
Size = 8192
Packed Size = 4096
Attributes = A_ -rw-r--r--
""";

        var entries = SevenZipOutputParser.ParseListOutput(output);

        Assert.Equal(3, entries.Count);
        Assert.Equal("docs", entries[0].Name);
        Assert.True(entries[0].IsDirectory);
        Assert.Equal("readme.txt", entries[1].Name);
        Assert.Equal("텍스트 문서", entries[1].TypeLabel);
        Assert.Equal(4096, entries[2].PackedSize);
    }

    [Fact]
    public void ParseListOutput_PreservesNamesAndAddsMissingParents()
    {
        var entries = SevenZipOutputParser.ParseListOutput("Path = 자료/하위/ 공백.txt \nSize = 12\n\nPath = 빈 폴더\nFolder = +\n");
        Assert.Contains(entries, e => e.Path == "자료/하위/ 공백.txt " && !e.IsDirectory);
        Assert.Contains(entries, e => e.Path == "자료" && e.IsDirectory);
        Assert.Contains(entries, e => e.Path == "자료/하위" && e.IsDirectory);
        Assert.Contains(entries, e => e.Path == "빈 폴더" && e.IsDirectory && e.TypeLabel == "폴더");
        Assert.Equal(4, entries.Count);
    }

    [Theory]
    [InlineData("sample.gz", ArchiveFormat.GZip)]
    [InlineData("sample.xz", ArchiveFormat.Xz)]
    [InlineData("sample.zst", ArchiveFormat.Zstd)]
    [InlineData("sample.bz2", ArchiveFormat.BZip2)]
    [InlineData("sample.tgz", ArchiveFormat.GZip)]
    [InlineData("sample.tbz2", ArchiveFormat.BZip2)]
    [InlineData("sample.txz", ArchiveFormat.Xz)]
    [InlineData("sample.tar.gz", ArchiveFormat.GZip)]
    [InlineData("sample.tar.bz2", ArchiveFormat.BZip2)]
    [InlineData("sample.tar.xz", ArchiveFormat.Xz)]
    public void DetectFormat_Recognizes_Compressed_Extensions(string path, ArchiveFormat expected)
    {
        Assert.Equal(expected, SevenZipArchiveBackend.DetectFormat(path));
    }
}
