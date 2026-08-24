namespace DigitalDevServices.Services.Logs;

internal static class LogFileTailReader
{
    public const int DefaultMaxLines = 100;
    public const int MaxAllowedLines = 10_000;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public static async Task<(string Content, int LinesRead)> ReadLastLinesAsync(
        string filePath,
        int maxLines,
        CancellationToken cancellationToken = default)
    {
        var normalizedMaxLines = NormalizeMaxLines(maxLines);
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length > MaxFileSizeBytes)
        {
            return await ReadTailFromLargeFileAsync(filePath, normalizedMaxLines, cancellationToken)
                .ConfigureAwait(false);
        }

        var lines = new List<string>(normalizedMaxLines);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (lines.Count == normalizedMaxLines)
            {
                lines.RemoveAt(0);
            }

            lines.Add(line);
        }

        return (string.Join(Environment.NewLine, lines), lines.Count);
    }

    private static async Task<(string Content, int LinesRead)> ReadTailFromLargeFileAsync(
        string filePath,
        int maxLines,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var startPosition = Math.Max(0, stream.Length - MaxFileSizeBytes);
        stream.Seek(startPosition, SeekOrigin.Begin);

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var lines = content
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .ToList();

        if (lines.Count > 0 && startPosition > 0)
        {
            lines.RemoveAt(0);
        }

        if (lines.Count > maxLines)
        {
            lines = lines.Skip(lines.Count - maxLines).ToList();
        }

        return (string.Join(Environment.NewLine, lines), lines.Count);
    }

    public static int NormalizeMaxLines(int maxLines) =>
        maxLines switch
        {
            <= 0 => DefaultMaxLines,
            > MaxAllowedLines => MaxAllowedLines,
            _ => maxLines
        };
}
