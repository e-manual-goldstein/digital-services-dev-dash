using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.DevDash.LogViewer;

internal static class LogEntryDisplay
{
    public static string FormatModalTitle(ParsedLogEntry entry)
    {
        var parts = new List<string>();

        if (entry.Timestamp is { } timestamp)
        {
            parts.Add(timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        if (!string.IsNullOrWhiteSpace(entry.Level))
        {
            parts.Add(entry.Level.Trim());
        }

        return parts.Count == 0 ? "Log entry" : string.Join(" · ", parts);
    }
}
