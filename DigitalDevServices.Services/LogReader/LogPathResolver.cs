namespace DigitalDevServices.Services.Logs;

internal static class LogPathResolver
{
    public static bool TryResolveLogFile(string? logPath, out string? logFilePath, out string? errorMessage)
    {
        logFilePath = null;
        errorMessage = null;

        var trimmed = logPath?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            errorMessage = "No log path is configured for this deployment.";
            return false;
        }

        if (File.Exists(trimmed))
        {
            logFilePath = trimmed;
            return true;
        }

        if (Directory.Exists(trimmed))
        {
            logFilePath = Directory
                .EnumerateFiles(trimmed, "*.log", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (logFilePath is null)
            {
                errorMessage = $"No .log files found in directory: {trimmed}";
                return false;
            }

            return true;
        }

        errorMessage = $"Log path does not exist: {trimmed}";
        return false;
    }
}
