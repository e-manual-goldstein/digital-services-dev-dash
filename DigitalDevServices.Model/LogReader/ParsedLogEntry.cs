namespace DigitalDevServices.Model.Logs;

public class ParsedLogEntry
{
    public DateTimeOffset? Timestamp { get; init; }

    public string? Level { get; init; }

    public required string Message { get; init; }

    public required string RawText { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}
