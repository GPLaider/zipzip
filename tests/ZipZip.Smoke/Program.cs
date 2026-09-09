using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using ZipZip.ArchiveAdapters.SevenZip;
using ZipZip.Domain.Models;
using Level = ZipZip.Domain.Models.CompressionLevel;

if (args.Length != 2 || !File.Exists(args[0]))
    throw new ArgumentException("Usage: ZipZip.Smoke <7z.exe> <scratch-directory>");
var root = Path.Combine(Path.GetFullPath(args[1]), "zipzip-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var backend = new SevenZipArchiveBackend(new SevenZipBackendOptions(Path.GetFullPath(args[0])));
var source = Path.Combine(root, "한글 파일.txt");
await File.WriteAllTextAsync(source, "ZipZip 한글과 공백 round trip");

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}
async Task Throws<T>(Func<Task> action, string message) where T : Exception
{
    try { await action().WaitAsync(TimeSpan.FromSeconds(15)); }
    catch (T) { Console.WriteLine("PASS " + message); return; }
    throw new Exception(message);
}

foreach (var format in new[] { ArchiveFormat.Zip, ArchiveFormat.SevenZip })
{
    var archive = Path.Combine(root, format == ArchiveFormat.Zip ? "결과.zip" : "결과.7z");
    var options = new CompressionOptions(archive, format, Level.Normal);
    await backend.CreateAsync([source], options);
    var summary = await backend.OpenAsync(archive);
    Check(summary.Entries.Any(e => e.Name == "한글 파일.txt"), format + " Unicode list");
    var destination = Path.Combine(root, format.ToString());
    await backend.ExtractAsync(archive, new ExtractionOptions(destination, false, false));
    Check(await File.ReadAllTextAsync(Path.Combine(destination, "한글 파일.txt")) == await File.ReadAllTextAsync(source), format + " round trip");
    var hash = SHA256.HashData(await File.ReadAllBytesAsync(archive));
    await Throws<IOException>(() => backend.CreateAsync([source], options), format + " existing output rejected");
    Check(hash.SequenceEqual(SHA256.HashData(await File.ReadAllBytesAsync(archive))), format + " existing output unchanged");
    await backend.TestAsync(archive);
    Check(hash.SequenceEqual(SHA256.HashData(await File.ReadAllBytesAsync(archive))), format + " integrity test leaves archive unchanged");
}

var nested = Path.Combine(root, "implicit.zip");
using (var zip = ZipFile.Open(nested, ZipArchiveMode.Create))
{
    using var writer = new StreamWriter(zip.CreateEntry("폴더/하위/문서.txt").Open());
    writer.Write("nested");
}
var nestedSummary = await backend.OpenAsync(nested);
Check(nestedSummary.Entries.Count(e => e.IsDirectory) == 2, "implicit folders visible");
var selectedOutput = Path.Combine(root, "selected");
await backend.ExtractAsync(nested, new ExtractionOptions(selectedOutput, false, false, SelectedEntries: ["폴더"]));
Check(File.Exists(Path.Combine(selectedOutput, "폴더", "하위", "문서.txt")), "selected folder extracts descendants");

var special = Path.Combine(root, "special.zip");
using (var zip = ZipFile.Open(special, ZipArchiveMode.Create))
{
    foreach (var name in new[] { "@list.txt", "-option.txt", "keep.txt" })
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(name);
    }
}
var specialOutput = Path.Combine(root, "special");
await backend.ExtractAsync(special, new ExtractionOptions(specialOutput, false, false, SelectedEntries: ["@list.txt", "-option.txt"]));
Check(File.Exists(Path.Combine(specialOutput, "@list.txt")) && File.Exists(Path.Combine(specialOutput, "-option.txt")) && !File.Exists(Path.Combine(specialOutput, "keep.txt")), "selection names are literal, not switches or list files");

var protectedZip = Path.Combine(root, "protected.zip");
await backend.CreateAsync([source], new CompressionOptions(protectedZip, ArchiveFormat.Zip, Level.Normal, Password: "smoke-only"));
var retryOutput = Path.Combine(root, "password-retry");
await Throws<InvalidOperationException>(() => backend.ExtractAsync(protectedZip, new ExtractionOptions(retryOutput, false, false)), "missing password does not hang");
await Throws<InvalidOperationException>(() => backend.ExtractAsync(protectedZip, new ExtractionOptions(retryOutput, false, false, Password: "wrong")), "wrong password rejected");
Check(!Directory.Exists(retryOutput), "wrong password writes no placeholder files");
await backend.ExtractAsync(protectedZip, new ExtractionOptions(retryOutput, false, false, Password: "smoke-only"));
Check(await File.ReadAllTextAsync(Path.Combine(retryOutput, "한글 파일.txt")) == await File.ReadAllTextAsync(source), "password retry recovers full content in same folder");

var protectedNames = Path.Combine(root, "protected-names.7z");
await backend.CreateAsync([source], new CompressionOptions(protectedNames, ArchiveFormat.SevenZip, Level.Normal, Password: "smoke-only", EncryptFileNames: true));
try
{
    await backend.OpenAsync(protectedNames);
    throw new Exception("Encrypted headers unexpectedly opened without a password");
}
catch (InvalidOperationException ex)
{
    Check(SevenZipErrorClassifier.RequiresPassword(ex.Message), "encrypted header error triggers GUI password prompt");
}
Check((await backend.OpenAsync(protectedNames, password: "smoke-only")).Entries.Any(e => e.Name == "한글 파일.txt"), "encrypted header list opens with password");
await Throws<InvalidOperationException>(() => backend.TestAsync(protectedNames, password: "wrong"), "integrity test rejects wrong password");
await backend.TestAsync(protectedNames, password: "smoke-only");
Check(true, "encrypted archive integrity test succeeds");

var large = Path.Combine(root, "random.bin");
await File.WriteAllBytesAsync(large, RandomNumberGenerator.GetBytes(16 * 1024 * 1024));
var split = Path.Combine(root, "split.7z");
var progressCheck = new RecordedProgress();
await backend.CreateAsync([large], new CompressionOptions(split, ArchiveFormat.SevenZip, Level.Fast, SplitSize: "1m"), progress: progressCheck);
Check(progressCheck.Values.Any(value => value is >= 0 and <= 100), "real engine progress delivered");
Check(File.Exists(split + ".001") && File.Exists(split + ".002"), "split volumes created");
Check((await backend.OpenAsync(split + ".001")).Entries.Any(e => e.Name == "random.bin"), "first split volume opens");

using (var cancellation = new CancellationTokenSource(100))
{
    var existing = Process.GetProcessesByName("7z").Select(p => { using (p) return p.Id; }).ToHashSet();
    await Throws<OperationCanceledException>(() => backend.CreateAsync([large], new CompressionOptions(Path.Combine(root, "cancelled.7z"), ArchiveFormat.SevenZip, Level.Maximum), cancellation.Token), "mid-operation cancellation");
    Check(!Process.GetProcessesByName("7z").Any(p => { using (p) return !existing.Contains(p.Id); }), "no orphaned 7-Zip process");
}
var corrupt = Path.Combine(root, "broken.zip");
await File.WriteAllTextAsync(corrupt, "not an archive");
await Throws<InvalidOperationException>(() => backend.OpenAsync(corrupt), "corrupt archive rejected");
await Throws<InvalidOperationException>(() => backend.TestAsync(corrupt), "integrity test rejects corrupt archive");
Console.WriteLine("Smoke evidence retained at " + root);

sealed class RecordedProgress : IProgress<int>
{
    public System.Collections.Concurrent.ConcurrentQueue<int> Values { get; } = new();
    public void Report(int value) => Values.Enqueue(value);
}
