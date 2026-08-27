using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalDevServices.Model.Logs;

public sealed class CustomRegexParserConfig
{
    public const string ModeEntry = "Entry";

    public const string ModeEntryStart = "EntryStart";

    public string Mode { get; init; } = ModeEntry;

    public string Pattern { get; init; } = string.Empty;

    public string? TimestampFormat { get; init; }

    public bool? Multiline { get; init; }

    [JsonIgnore]
    public bool IsEntryStart =>
        string.Equals(Mode, ModeEntryStart, StringComparison.OrdinalIgnoreCase)
        || Multiline == true;

    public static CustomRegexParserConfig Parse(string? parserConfigJson)
    {
        if (string.IsNullOrWhiteSpace(parserConfigJson) || parserConfigJson.Trim() == "{}")
        {
            throw new InvalidOperationException("Custom regex parser config is required.");
        }

        try
        {
            var config = JsonSerializer.Deserialize<CustomRegexParserConfig>(parserConfigJson, JsonOptions)
                ?? throw new InvalidOperationException("Custom regex parser config is invalid.");

            if (string.IsNullOrWhiteSpace(config.Pattern))
            {
                throw new InvalidOperationException("Custom regex pattern is required.");
            }

            var mode = config.Mode?.Trim();
            if (string.IsNullOrWhiteSpace(mode))
            {
                mode = config.Multiline == true ? ModeEntryStart : ModeEntry;
            }

            if (!string.Equals(mode, ModeEntry, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(mode, ModeEntryStart, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Custom regex mode must be '{ModeEntry}' or '{ModeEntryStart}'.");
            }

            return new CustomRegexParserConfig
            {
                Mode = string.Equals(mode, ModeEntryStart, StringComparison.OrdinalIgnoreCase)
                    ? ModeEntryStart
                    : ModeEntry,
                Pattern = config.Pattern.Trim(),
                TimestampFormat = string.IsNullOrWhiteSpace(config.TimestampFormat)
                    ? null
                    : config.TimestampFormat.Trim(),
                Multiline = config.Multiline
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Custom regex parser config is not valid JSON: {ex.Message}", ex);
        }
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
