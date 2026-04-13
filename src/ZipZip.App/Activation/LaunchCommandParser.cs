namespace ZipZip.App.Activation;

public static class LaunchCommandParser
{
    public static LaunchCommand Parse(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return LaunchCommand.None;
        }

        return ParseTokens(Tokenize(arguments));
    }

    public static LaunchCommand Parse(IReadOnlyList<string> tokens)
    {
        return ParseTokens(tokens);
    }

    private static LaunchCommand ParseTokens(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return LaunchCommand.None;
        }

        if (IsOption(tokens[0], "--open"))
        {
            return tokens.Count >= 2
                ? new LaunchCommand(LaunchAction.OpenArchive, [tokens[1]])
                : LaunchCommand.None;
        }

        if (IsOption(tokens[0], "--create"))
        {
            var paths = tokens.Skip(1).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
            return paths.Length > 0
                ? new LaunchCommand(LaunchAction.CreateArchive, paths)
                : LaunchCommand.None;
        }

        if (IsOption(tokens[0], "--settings"))
        {
            return new LaunchCommand(LaunchAction.ShowSettings, []);
        }

        return File.Exists(tokens[0])
            ? new LaunchCommand(LaunchAction.OpenArchive, [tokens[0]])
            : LaunchCommand.None;
    }

    private static bool IsOption(string token, string expected)
    {
        return string.Equals(token, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> Tokenize(string arguments)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var character in arguments)
        {
            switch (character)
            {
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case ' ' when !inQuotes:
                    AppendToken(result, current);
                    break;
                default:
                    current.Append(character);
                    break;
            }
        }

        AppendToken(result, current);
        return result;
    }

    private static void AppendToken(List<string> tokens, System.Text.StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return;
        }

        tokens.Add(builder.ToString());
        builder.Clear();
    }
}
