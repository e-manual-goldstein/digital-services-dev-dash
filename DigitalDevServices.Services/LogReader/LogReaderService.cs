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

    public async Task<LogReadResult> ReadAsync(
        Guid applicationInstanceId,
        int maxLines = LogFileTailReader.DefaultMaxLines,
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

        if (!LogPathResolver.TryResolveLogFile(instance.LogPath, out var logFilePath, out var resolveError))
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
                .ReadLastLinesAsync(logFilePath!, maxLines, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new LogReadResult
            {
                LogFilePath = logFilePath,
                ErrorMessage = $"Could not read log file '{logFilePath}': {ex.Message}"
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
                LogFilePath = logFilePath,
                RawLinesRead = rawLinesRead,
                ErrorMessage = ex.Message
            };
        }

        return new LogReadResult
        {
            Entries = entries,
            LogFilePath = logFilePath,
            RawLinesRead = rawLinesRead
        };
    }
}
