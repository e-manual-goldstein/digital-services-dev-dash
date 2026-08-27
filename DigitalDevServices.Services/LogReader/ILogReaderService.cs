using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Logs;

public interface ILogReaderService
{
    Task<LogFileListResult> ListLogFilesAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default);

    Task<LogReadResult> ReadAsync(
        Guid applicationInstanceId,
        int maxLines = LogFileTailReader.DefaultMaxLines,
        string? logFilePath = null,
        CancellationToken cancellationToken = default);
}
