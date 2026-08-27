namespace DigitalDevServices.Model.Logs;

public sealed class AvailableLogFile
{
    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public long SizeBytes { get; init; }

    public DateTimeOffset LastModifiedUtc { get; init; }
}
