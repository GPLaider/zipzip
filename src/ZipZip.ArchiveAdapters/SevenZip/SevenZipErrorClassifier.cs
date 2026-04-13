namespace ZipZip.ArchiveAdapters.SevenZip;

public static class SevenZipErrorClassifier
{
    private static readonly string[] PasswordMarkers =
    [
        "password",
        "wrong password",
        "encrypted",
        "can not open encrypted archive",
        "headers error",
        "암호",
        "비밀번호",
        "암호화",
        "잘못된 암호",
        "암호가 필요",
    ];

    public static bool RequiresPassword(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = message.ToLowerInvariant();
        return PasswordMarkers.Any(normalized.Contains);
    }
}
