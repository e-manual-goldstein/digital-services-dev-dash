namespace DigitalDevServices.Model.Logs;

public static class LogFormatNames
{
    public const string SerilogJson = "SerilogJson";

    public const string PlainText = "PlainText";

    public const string NLogMultiline = "NLogMultiline";

    public const string Log4NetPattern = "Log4NetPattern";

    public static IReadOnlyList<string> All { get; } =
    [
        SerilogJson,
        PlainText,
        NLogMultiline,
        Log4NetPattern
    ];

    public static string GetDisplayName(string formatName) => formatName switch
    {
        SerilogJson => "Serilog JSON",
        PlainText => "Plain text",
        NLogMultiline => "NLog multiline",
        Log4NetPattern => "log4net pattern",
        _ => formatName
    };
}
