namespace ZipZip.Domain.Models;

public sealed record ExtractionOptions(
    string DestinationPath,
    bool CreateNewFolder,
    bool OverwriteExisting,
    string? Password = null,
    IReadOnlyList<string>? SelectedEntries = null);
