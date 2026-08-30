using System.Text.Json;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed class SerilogJsonLogParser : ILogEntryParser
{
    private static readonly string[] ReservedPropertyNames =
    [
        "@timestamp",
        "message",
        "log.level",
        "error",
        "error.message",
        "error.stack_trace",
        "error.type",
        "error_message",
        "error_stack_trace",
        "error_type",
        "level",
        "@t",
        "@l",
        "@mt",
        "@m",
        "@x",
        "@r"
    ];

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

                var timestamp = TryGetTimestamp(root);
                var level = TryGetLevel(root);
                var message = TryGetMessage(root) ?? line;
                var exception = SerilogExceptionExtractor.TryExtract(root);

                var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in root.EnumerateObject())
                {
                    if (IsReservedProperty(property.Name))
                    {
                        continue;
                    }

                    properties[property.Name] = property.Value.ToString();
                }

                entries.Add(new ParsedLogEntry
                {
                    Timestamp = timestamp,
                    Level = level,
                    Message = message,
                    RawText = line,
                    Exception = exception,
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

    private static DateTimeOffset? TryGetTimestamp(JsonElement root)
    {
        foreach (var propertyName in new[] { "@timestamp", "@t" })
        {
            if (TryGetString(root, propertyName, out var value)
                && DateTimeOffset.TryParse(value, out var parsedTimestamp))
            {
                return parsedTimestamp;
            }
        }

        return null;
    }

    private static string? TryGetLevel(JsonElement root)
    {
        foreach (var propertyName in new[] { "log.level", "level", "@l" })
        {
            if (TryGetString(root, propertyName, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? TryGetMessage(JsonElement root)
    {
        foreach (var propertyName in new[] { "message", "@m", "@mt" })
        {
            if (TryGetString(root, propertyName, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool IsReservedProperty(string propertyName) =>
        ReservedPropertyNames.Contains(propertyName, StringComparer.OrdinalIgnoreCase);

    private static bool TryGetString(JsonElement root, string propertyName, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return false;
        }

        value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.ToString()
        };

        return !string.IsNullOrWhiteSpace(value);
    }
}
