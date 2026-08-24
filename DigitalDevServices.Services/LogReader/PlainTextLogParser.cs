using System.Text.RegularExpressions;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed partial class PlainTextLogParser : ILogEntryParser
{
    [GeneratedRegex(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+(?<level>[A-Z]+)\s+\[(?<logger>[^\]]+)\]\s+(?<message>.*)$",
        RegexOptions.Compiled)]
    private static partial Regex EntryPattern();

    public string FormatName => "PlainText";

    public IReadOnlyList<ParsedLogEntry> Parse(string content)
    {
        var entries = new List<ParsedLogEntry>();

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = EntryPattern().Match(line);
            if (!match.Success)
            {
                continue;
            }

            DateTimeOffset? timestamp = DateTimeOffset.TryParse(match.Groups["timestamp"].Value, out var parsedTimestamp)
                ? parsedTimestamp
                : null;

            entries.Add(new ParsedLogEntry
            {
                Timestamp = timestamp,
                Level = match.Groups["level"].Value,
                Message = match.Groups["message"].Value,
                RawText = line,
                Properties = new Dictionary<string, string>
                {
                    ["Logger"] = match.Groups["logger"].Value
                }
            });
        }

        return entries;
    }
}
