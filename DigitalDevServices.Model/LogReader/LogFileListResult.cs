namespace DigitalDevServices.Model.Logs;

public class LogFileListResult
{
    public IReadOnlyList<AvailableLogFile> Files { get; init; } = [];

    public string? ConfiguredLogPath { get; init; }

    public bool IsDirectory { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
