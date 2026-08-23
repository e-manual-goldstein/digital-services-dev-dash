using System.Text.Json;

namespace DigitalDevServices.Services.Configuration;

internal static class JsonConfigurationFlattener
{
    public static IReadOnlyDictionary<string, string> Flatten(string jsonContent)
    {
        using var document = JsonDocument.Parse(jsonContent);
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        FlattenElement(document.RootElement, prefix: string.Empty, results);
        return results;
    }

    private static void FlattenElement(
        JsonElement element,
        string prefix,
        Dictionary<string, string> results)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}:{property.Name}";
                    FlattenElement(property.Value, key, results);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenElement(item, $"{prefix}:{index}", results);
                    index++;
                }

                break;

            default:
                if (string.IsNullOrEmpty(prefix))
                {
                    break;
                }

                results[prefix] = GetScalarValue(element);
                break;
        }
    }

    private static string GetScalarValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
}
