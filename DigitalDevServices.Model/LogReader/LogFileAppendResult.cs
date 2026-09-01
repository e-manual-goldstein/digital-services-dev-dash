namespace DigitalDevServices.Model.Logs;

public sealed class LogFileAppendResult
{
    public string Content { get; init; } = string.Empty;

    public long StartPosition { get; init; }

    public long EndPosition { get; init; }

    public bool WasTruncated { get; init; }

    public bool HasNewContent => Content.Length > 0;
}
