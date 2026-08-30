using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed class CustomRegexLogParser
{
    private static readonly HashSet<string> ReservedGroupNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "message",
        "timestamp",
        "level"
    };

    public IReadOnlyList<ParsedLogEntry> Parse(string content, CustomRegexParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        CustomRegexParserConfigValidator.Validate(config);

        var regex = CustomRegexParserConfigValidator.CompilePattern(config.Pattern);
        return config.IsEntryStart
            ? ParseEntryStart(content, regex, config.TimestampFormat)
            : ParseEntry(content, regex, config.TimestampFormat);
    }

    private static IReadOnlyList<ParsedLogEntry> ParseEntry(string content, Regex regex, string? timestampFormat)
    {
        var entries = new List<ParsedLogEntry>();

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var match = regex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            entries.Add(CreateEntry(match, line, timestampFormat));
        }

        return entries;
    }

    private static IReadOnlyList<ParsedLogEntry> ParseEntryStart(string content, Regex regex, string? timestampFormat)
    {
        var entries = new List<ParsedLogEntry>();
        EntryBuilder? current = null;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                current?.AppendLine(string.Empty);
                continue;
            }

            var match = regex.Match(line);
            if (match.Success)
            {
                current?.Build(entries, timestampFormat);
                current = new EntryBuilder(match, line);
            }
            else
            {
                current?.AppendLine(line);
            }
        }

        current?.Build(entries, timestampFormat);
        return entries;
    }

    private static ParsedLogEntry CreateEntry(Match match, string rawText, string? timestampFormat)
    {
        var properties = ExtractProperties(match);

        return new ParsedLogEntry
        {
            Timestamp = ParseTimestamp(match.Groups["timestamp"], timestampFormat),
            Level = GetOptionalGroupValue(match, "level"),
            Message = match.Groups["message"].Value,
            RawText = rawText,
            Properties = properties.Count == 0 ? null : properties
        };
    }

    private static DateTimeOffset? ParseTimestamp(Group timestampGroup, string? timestampFormat)
    {
        if (!timestampGroup.Success)
        {
            return null;
        }

        var value = timestampGroup.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(timestampFormat)
            && DateTimeOffset.TryParseExact(
                value,
                timestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var exactTimestamp))
        {
            return exactTimestamp;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var parsedTimestamp)
            ? parsedTimestamp
            : null;
    }

    private static string? GetOptionalGroupValue(Match match, string groupName)
    {
        var group = match.Groups[groupName];
        return group.Success && !string.IsNullOrWhiteSpace(group.Value) ? group.Value : null;
    }

    private static Dictionary<string, string> ExtractProperties(Match match)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupName in match.Groups.Keys.Cast<string>())
        {
            if (ReservedGroupNames.Contains(groupName) || int.TryParse(groupName, out _))
            {
                continue;
            }

            var group = match.Groups[groupName];
            if (group.Success && !string.IsNullOrWhiteSpace(group.Value))
            {
                properties[groupName] = group.Value;
            }
        }

        return properties;
    }

    private sealed class EntryBuilder
    {
        private readonly Match _match;
        private readonly StringBuilder _message = new();
        private readonly StringBuilder _rawText = new();

        public EntryBuilder(Match match, string headerLine)
        {
            _match = match;
            _message.Append(match.Groups["message"].Value);
            _rawText.Append(headerLine);
        }

        public void AppendLine(string line)
        {
            _message.AppendLine();
            _message.Append(line);
            _rawText.AppendLine();
            _rawText.Append(line);
        }

        public void Build(List<ParsedLogEntry> entries, string? timestampFormat)
        {
            var properties = ExtractProperties(_match);
            var fullMessage = _message.ToString();
            var (message, exception) = LogEntryExceptionSplitter.Split(fullMessage);

            entries.Add(new ParsedLogEntry
            {
                Timestamp = ParseTimestamp(_match.Groups["timestamp"], timestampFormat),
                Level = GetOptionalGroupValue(_match, "level"),
                Message = message,
                RawText = _rawText.ToString(),
                Exception = exception,
                Properties = properties.Count == 0 ? null : properties
            });
        }
    }
}
