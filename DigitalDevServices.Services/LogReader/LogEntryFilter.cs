using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public static class LogEntryFilter
{
    public const string MinimumLevelAll = "";

    public const string MinimumLevelDebug = "Debug";

    public const string MinimumLevelInformation = "Information";

    public const string MinimumLevelWarning = "Warning";

    public const string MinimumLevelError = "Error";

    public static IReadOnlyList<(string Value, string Label)> MinimumLevelOptions { get; } =
    [
        (MinimumLevelAll, "All levels"),
        (MinimumLevelDebug, "Debug and above"),
        (MinimumLevelInformation, "Info and above"),
        (MinimumLevelWarning, "Warning and above"),
        (MinimumLevelError, "Error and above")
    ];

    public static IReadOnlyList<ParsedLogEntry> Apply(
        IEnumerable<ParsedLogEntry> entries,
        string? minimumLevel,
        string? messageContains)
    {
        return entries.Where(entry => Matches(entry, minimumLevel, messageContains)).ToList();
    }

    public static bool Matches(ParsedLogEntry entry, string? minimumLevel, string? messageContains)
    {
        if (!MatchesMinimumLevel(entry.Level, minimumLevel))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(messageContains))
        {
            return true;
        }

        return entry.Message.Contains(messageContains.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesMinimumLevel(string? entryLevel, string? minimumLevel)
    {
        if (string.IsNullOrWhiteSpace(minimumLevel))
        {
            return true;
        }

        var entrySeverity = TryGetSeverity(entryLevel);
        var minimumSeverity = GetMinimumSeverity(minimumLevel);

        return entrySeverity is not null && entrySeverity >= minimumSeverity;
    }

    public static int? TryGetSeverity(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return null;
        }

        return level.ToUpperInvariant() switch
        {
            "TRACE" or "VERBOSE" => 0,
            "DEBUG" => 1,
            "INFO" or "INFORMATION" => 2,
            "WARN" or "WARNING" => 3,
            "ERROR" or "ERR" => 4,
            "FATAL" or "CRITICAL" => 5,
            _ => null
        };
    }

    private static int GetMinimumSeverity(string minimumLevel) =>
        minimumLevel switch
        {
            MinimumLevelDebug => 1,
            MinimumLevelInformation => 2,
            MinimumLevelWarning => 3,
            MinimumLevelError => 4,
            _ => 0
        };
}
