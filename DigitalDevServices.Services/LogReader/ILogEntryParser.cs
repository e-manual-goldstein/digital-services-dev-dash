using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public interface ILogEntryParser
{
    string FormatName { get; }

    IReadOnlyList<ParsedLogEntry> Parse(string content);
}
