using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

internal static class LogPathResolver
{
    public static bool TryListLogFiles(string? logPath, out LogFileListResult result)
    {
        result = new LogFileListResult();

        var trimmed = logPath?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            result = new LogFileListResult
            {
                ErrorMessage = "No log path is configured for this deployment."
            };
            return false;
        }

        if (File.Exists(trimmed))
        {
            result = new LogFileListResult
            {
                ConfiguredLogPath = trimmed,
                IsDirectory = false,
                Files = [CreateAvailableLogFile(trimmed)]
            };
            return true;
        }

        if (Directory.Exists(trimmed))
        {
            var files = EnumerateLogFiles(trimmed);
            if (files.Count == 0)
            {
                result = new LogFileListResult
                {
                    ConfiguredLogPath = trimmed,
                    IsDirectory = true,
                    ErrorMessage = $"No .log files found in directory: {trimmed}"
                };
                return false;
            }

            result = new LogFileListResult
            {
                ConfiguredLogPath = trimmed,
                IsDirectory = true,
                Files = files
            };
            return true;
        }

        result = new LogFileListResult
        {
            ConfiguredLogPath = trimmed,
            ErrorMessage = $"Log path does not exist: {trimmed}"
        };
        return false;
    }

    public static bool TryResolveLogFile(
        string? configuredLogPath,
        string? explicitLogFilePath,
        out string? logFilePath,
        out string? errorMessage)
    {
        logFilePath = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(explicitLogFilePath))
        {
            return TryResolveDefaultLogFile(configuredLogPath, out logFilePath, out errorMessage);
        }

        var selectedPath = explicitLogFilePath.Trim();
        if (!File.Exists(selectedPath))
        {
            errorMessage = $"Log file does not exist: {selectedPath}";
            return false;
        }

        var configured = configuredLogPath?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            errorMessage = "No log path is configured for this deployment.";
            return false;
        }

        if (File.Exists(configured))
        {
            if (!PathsEqual(configured, selectedPath))
            {
                errorMessage = "Selected log file does not match the configured log path.";
                return false;
            }

            logFilePath = selectedPath;
            return true;
        }

        if (Directory.Exists(configured))
        {
            if (!IsFileInDirectory(selectedPath, configured))
            {
                errorMessage = "Selected log file is not in the configured log directory.";
                return false;
            }

            if (!selectedPath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Selected file is not a .log file.";
                return false;
            }

            logFilePath = selectedPath;
            return true;
        }

        errorMessage = $"Log path does not exist: {configured}";
        return false;
    }

    private static bool TryResolveDefaultLogFile(string? logPath, out string? logFilePath, out string? errorMessage)
    {
        logFilePath = null;
        errorMessage = null;

        if (!TryListLogFiles(logPath, out var listResult))
        {
            errorMessage = listResult.ErrorMessage;
            return false;
        }

        logFilePath = listResult.Files[0].FilePath;
        return true;
    }

    private static IReadOnlyList<AvailableLogFile> EnumerateLogFiles(string directoryPath) =>
        Directory
            .EnumerateFiles(directoryPath, "*.log", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(CreateAvailableLogFile)
            .ToList();

    private static AvailableLogFile CreateAvailableLogFile(string filePath)
    {
        var info = new FileInfo(filePath);
        return new AvailableLogFile
        {
            FilePath = filePath,
            FileName = info.Name,
            SizeBytes = info.Length,
            LastModifiedUtc = info.LastWriteTimeUtc
        };
    }

    private static bool IsFileInDirectory(string filePath, string directoryPath)
    {
        var fullDirectory = Path.GetFullPath(directoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullFile = Path.GetFullPath(filePath);
        var prefix = fullDirectory + Path.DirectorySeparatorChar;
        return fullFile.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
}
