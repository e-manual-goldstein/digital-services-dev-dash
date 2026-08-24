using System.Text.Json;
using System.Text.RegularExpressions;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed partial class SerilogJsonLogParser : ILogEntryParser
{
    public string FormatName => "SerilogJson";

    public IReadOnlyList<ParsedLogEntry> Parse(string content)
    {
        var entries = new List<ParsedLogEntry>();

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                DateTimeOffset? timestamp = null;
                if (root.TryGetProperty("@t", out var timestampElement)
                    && DateTimeOffset.TryParse(timestampElement.GetString(), out var parsedTimestamp))
                {
                    timestamp = parsedTimestamp;
                }

                var level = root.TryGetProperty("@l", out var levelElement)
                    ? levelElement.GetString()
                    : null;

                var message = root.TryGetProperty("@mt", out var messageElement)
                    ? messageElement.GetString() ?? line
                    : line;

                var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name is "@t" or "@l" or "@mt" or "@x" or "@r")
                    {
                        continue;
                    }

                    properties[property.Name] = property.Value.ToString();
                }

                if (root.TryGetProperty("@x", out var exceptionElement))
                {
                    var exception = exceptionElement.GetString();
                    if (!string.IsNullOrWhiteSpace(exception))
                    {
                        message = $"{message}\n{exception}";
                    }
                }

                entries.Add(new ParsedLogEntry
                {
                    Timestamp = timestamp,
                    Level = level,
                    Message = message,
                    RawText = line,
                    Properties = properties.Count == 0 ? null : properties
                });
            }
            catch (JsonException)
            {
                entries.Add(new ParsedLogEntry
                {
                    Level = "WARN",
                    Message = "Could not parse JSON log line.",
                    RawText = line
                });
            }
        }

        return entries;
    }
}
