namespace ZipZip.App.Activation;

public enum LaunchAction
{
    None = 0,
    OpenArchive,
    CreateArchive,
    ShowSettings,
}

public sealed record LaunchCommand(LaunchAction Action, IReadOnlyList<string> Paths)
{
    public static LaunchCommand None { get; } = new(LaunchAction.None, []);
}
