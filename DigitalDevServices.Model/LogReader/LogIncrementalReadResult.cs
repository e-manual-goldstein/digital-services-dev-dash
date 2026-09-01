namespace DigitalDevServices.Model.Logs;

public sealed class LogIncrementalReadResult
{
    public string? LogFilePath { get; init; }

    public string? ErrorMessage { get; init; }

    public string NewRawContent { get; init; } = string.Empty;

    public long TailBytePosition { get; init; }

    public bool WasTruncated { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
