namespace DigitalDevServices.Model.Logs;

public class LogReadResult
{
    public IReadOnlyList<ParsedLogEntry> Entries { get; init; } = [];

    public string? LogFilePath { get; init; }

    public int RawLinesRead { get; init; }

    public string? RawContent { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
