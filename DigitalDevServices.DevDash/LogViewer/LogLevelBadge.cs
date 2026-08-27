namespace DigitalDevServices.DevDash.LogViewer;

internal static class LogLevelBadge
{
    public static string GetClass(string? level) =>
        level?.ToUpperInvariant() switch
        {
            "ERROR" or "ERR" or "FATAL" or "CRITICAL" => "bg-danger",
            "WARN" or "WARNING" => "bg-warning text-dark",
            "INFO" or "INFORMATION" => "bg-info text-dark",
            "DEBUG" or "TRACE" => "bg-secondary",
            _ => "bg-light text-dark border"
        };
}
