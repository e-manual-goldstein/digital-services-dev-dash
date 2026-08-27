using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Logs;

public sealed class LogReaderService : ILogReaderService
{
    private readonly IApplicationInstanceService _applicationInstanceService;
    private readonly ILogParsingService _logParsingService;

    public LogReaderService(
        IApplicationInstanceService applicationInstanceService,
        ILogParsingService logParsingService)
    {
        _applicationInstanceService = applicationInstanceService;
        _logParsingService = logParsingService;
    }

    public async Task<LogFileListResult> ListLogFilesAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await _applicationInstanceService
            .GetByIdAsync(applicationInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            return new LogFileListResult
            {
                ErrorMessage = "Application instance was not found."
            };
        }

        LogPathResolver.TryListLogFiles(instance.LogPath, out var result);
        return result;
    }

    public async Task<LogReadResult> ReadAsync(
        Guid applicationInstanceId,
        int maxLines = LogFileTailReader.DefaultMaxLines,
        string? logFilePath = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await _applicationInstanceService
            .GetByIdAsync(applicationInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            return new LogReadResult
            {
                ErrorMessage = "Application instance was not found."
            };
        }

        if (!LogPathResolver.TryResolveLogFile(instance.LogPath, logFilePath, out var resolvedLogFilePath, out var resolveError))
        {
            return new LogReadResult
            {
                ErrorMessage = resolveError
            };
        }

        string content;
        int rawLinesRead;

        try
        {
            (content, rawLinesRead) = await LogFileTailReader
                .ReadLastLinesAsync(resolvedLogFilePath!, maxLines, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new LogReadResult
            {
                LogFilePath = resolvedLogFilePath,
                ErrorMessage = $"Could not read log file '{resolvedLogFilePath}': {ex.Message}"
            };
        }

        IReadOnlyList<ParsedLogEntry> entries;

        try
        {
            entries = await _logParsingService
                .ParseForDeployableApplicationAsync(instance.DeployableApplicationId, content, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return new LogReadResult
            {
                LogFilePath = resolvedLogFilePath,
                RawLinesRead = rawLinesRead,
                ErrorMessage = ex.Message
            };
        }

        return new LogReadResult
        {
            Entries = entries,
            LogFilePath = resolvedLogFilePath,
            RawLinesRead = rawLinesRead
        };
    }
}
