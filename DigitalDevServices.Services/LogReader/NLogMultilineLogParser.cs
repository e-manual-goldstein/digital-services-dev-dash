using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed partial class NLogMultilineLogParser : ILogEntryParser
{
    [GeneratedRegex(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d+)?)\|(?<level>[A-Z]+)\|(?<logger>[^|]+)\|(?<message>.*)$",
        RegexOptions.Compiled)]
    private static partial Regex EntryStartPattern();

    public string FormatName => "NLogMultiline";

    public IReadOnlyList<ParsedLogEntry> Parse(string content)
    {
        var entries = new List<ParsedLogEntry>();
        ParsedLogEntryBuilder? current = null;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                current?.AppendLine(string.Empty);
                continue;
            }

            var match = EntryStartPattern().Match(line);
            if (match.Success)
            {
                current?.Build(entries);
                current = new ParsedLogEntryBuilder(
                    match.Groups["timestamp"].Value,
                    match.Groups["level"].Value,
                    match.Groups["logger"].Value,
                    match.Groups["message"].Value,
                    line);
            }
            else
            {
                current?.AppendLine(line);
            }
        }

        current?.Build(entries);
        return entries;
    }

    private sealed class ParsedLogEntryBuilder
    {
        private readonly string _timestampText;
        private readonly string _level;
        private readonly string _logger;
        private readonly StringBuilder _message = new();
        private readonly StringBuilder _rawText = new();

        public ParsedLogEntryBuilder(string timestampText, string level, string logger, string initialMessage, string rawHeaderLine)
        {
            _timestampText = timestampText;
            _level = level;
            _logger = logger;
            _message.Append(initialMessage);
            _rawText.Append(rawHeaderLine);
        }

        public void AppendLine(string line)
        {
            _message.AppendLine();
            _message.Append(line);
            _rawText.AppendLine();
            _rawText.Append(line);
        }

        public void Build(List<ParsedLogEntry> entries)
        {
            var fullMessage = _message.ToString();
            var (message, exception) = LogEntryExceptionSplitter.Split(fullMessage);

            DateTimeOffset? timestamp = DateTimeOffset.TryParse(
                _timestampText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsedTimestamp)
                ? parsedTimestamp
                : null;

            entries.Add(new ParsedLogEntry
            {
                Timestamp = timestamp,
                Level = _level,
                Message = message,
                RawText = _rawText.ToString(),
                Exception = exception,
                Properties = new Dictionary<string, string>
                {
                    ["Logger"] = _logger
                }
            });
        }
    }
}
