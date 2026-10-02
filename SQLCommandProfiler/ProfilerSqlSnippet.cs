namespace SQLCommandProfiler;

public static class ProfilerSqlSnippet
{
    private const int DefaultTileMaxLength = 160;

    public static string FormatForTile(string sqlText, int maxLength = DefaultTileMaxLength)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            return string.Empty;
        }

        var singleLine = sqlText
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();

        while (singleLine.Contains("  ", StringComparison.Ordinal))
        {
            singleLine = singleLine.Replace("  ", " ", StringComparison.Ordinal);
        }

        if (singleLine.Length <= maxLength)
        {
            return singleLine;
        }

        return string.Concat(singleLine.AsSpan(0, maxLength - 3), "...");
    }
}
