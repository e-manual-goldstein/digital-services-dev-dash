using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Logs;

public interface ILogReaderService
{
    Task<LogReadResult> ReadAsync(
        Guid applicationInstanceId,
        int maxLines = LogFileTailReader.DefaultMaxLines,
        CancellationToken cancellationToken = default);
}
