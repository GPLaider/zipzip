namespace ZipZip.ArchiveAdapters.SevenZip;

public static class SevenZipErrorClassifier
{
    private static readonly string[] PasswordMarkers =
    [
        "password",
        "wrong password",
        "encrypted",
        "can not open encrypted archive",
        "data error in encrypted file",
        "headers error",
        "enter password",
        "암호",
        "비밀번호",
        "잘못된 암호",
        "잘못된 비밀번호",
        "암호가 필요",
        "비밀번호가 필요",
        "암호화",
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
