using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed class LogParserRegistry
{
    private readonly IReadOnlyDictionary<string, ILogEntryParser> _parsersByFormat;

    public LogParserRegistry(IEnumerable<ILogEntryParser> parsers)
    {
        _parsersByFormat = parsers.ToDictionary(parser => parser.FormatName, StringComparer.OrdinalIgnoreCase);
    }

    public ILogEntryParser GetRequiredParser(string formatName)
    {
        if (_parsersByFormat.TryGetValue(formatName, out var parser))
        {
            return parser;
        }

        throw new InvalidOperationException($"No log parser registered for format '{formatName}'.");
    }

    public bool TryGetParser(string formatName, out ILogEntryParser? parser)
    {
        return _parsersByFormat.TryGetValue(formatName, out parser);
    }

    public IReadOnlyList<string> GetRegisteredFormatNames()
    {
        return _parsersByFormat.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
