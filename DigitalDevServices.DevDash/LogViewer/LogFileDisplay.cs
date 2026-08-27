using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.DevDash.LogViewer;

internal static class LogFileDisplay
{
    public static string FormatFileSize(long bytes) =>
        bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.#} MB"
        };

    public static string FormatOptionLabel(AvailableLogFile file) =>
        $"{file.FileName} ({FormatFileSize(file.SizeBytes)}, {file.LastModifiedUtc.ToLocalTime():yyyy-MM-dd HH:mm})";
}
