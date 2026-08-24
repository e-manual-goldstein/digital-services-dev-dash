namespace DigitalDevServices.Plugins.Logs;

/// <summary>
/// Marker for log parser implementations that can be registered in DI as <c>ILogEntryParser</c>.
/// Parser types live in Services today; additional formats can be added via plugins later.
/// </summary>
public interface ILogParserPlugin
{
    string FormatName { get; }
}
