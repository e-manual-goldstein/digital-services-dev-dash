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
        string? formatName = null,
        string? parserConfig = null,
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
            entries = !string.IsNullOrWhiteSpace(formatName)
                ? _logParsingService.ParseWithFormat(formatName, content, parserConfig)
                : await _logParsingService
                    .ParseForDeployableApplicationAsync(instance.DeployableApplicationId, content, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return new LogReadResult
            {
                LogFilePath = resolvedLogFilePath,
                RawLinesRead = rawLinesRead,
                RawContent = content,
                ErrorMessage = ex.Message
            };
        }

        return new LogReadResult
        {
            Entries = entries,
            LogFilePath = resolvedLogFilePath,
            RawLinesRead = rawLinesRead,
            RawContent = content,
            TailBytePosition = LogFileTailReader.GetFileLength(resolvedLogFilePath!)
        };
    }

    public async Task<LogIncrementalReadResult> ReadIncrementalAsync(
        Guid applicationInstanceId,
        long startPosition,
        string? logFilePath = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await _applicationInstanceService
            .GetByIdAsync(applicationInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            return new LogIncrementalReadResult
            {
                ErrorMessage = "Application instance was not found."
            };
        }

        if (!LogPathResolver.TryResolveLogFile(instance.LogPath, logFilePath, out var resolvedLogFilePath, out var resolveError))
        {
            return new LogIncrementalReadResult
            {
                ErrorMessage = resolveError
            };
        }

        try
        {
            var appendResult = await LogFileTailReader
                .ReadAppendAsync(resolvedLogFilePath!, startPosition, cancellationToken)
                .ConfigureAwait(false);

            return new LogIncrementalReadResult
            {
                LogFilePath = resolvedLogFilePath,
                NewRawContent = appendResult.Content,
                TailBytePosition = appendResult.EndPosition,
                WasTruncated = appendResult.WasTruncated
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new LogIncrementalReadResult
            {
                LogFilePath = resolvedLogFilePath,
                ErrorMessage = $"Could not read log file '{resolvedLogFilePath}': {ex.Message}"
            };
        }
    }
}
