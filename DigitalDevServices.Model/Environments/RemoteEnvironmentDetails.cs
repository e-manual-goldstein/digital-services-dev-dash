using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Environment details returned by the external team's Web API.
/// </summary>
public class RemoteEnvironmentDetails
{
    public string Code { get; set; } = string.Empty;

    public string EnvironmentType { get; set; } = string.Empty;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public EnvironmentServer[] Servers { get; set; } = [];

    public EnvironmentWindowsService[] WindowsServices { get; set; } = [];

    /// <summary>
    /// JSON properties not mapped to first-class members. Populated via <see cref="JsonExtensionDataAttribute"/>.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    private static readonly JsonSerializerOptions PrettyJsonOptions = new() { WriteIndented = true };

    public bool HasAdditionalProperties => AdditionalProperties is { Count: > 0 };

    public string? FormatAdditionalPropertiesJson()
    {
        if (!HasAdditionalProperties)
        {
            return null;
        }

        return JsonSerializer.Serialize(AdditionalProperties, PrettyJsonOptions);
    }

    public bool TryGetAdditionalString(string propertyName, out string? value)
    {
        value = null;
        if (AdditionalProperties is null
            || !AdditionalProperties.TryGetValue(propertyName, out var element))
        {
            return false;
        }

        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        value = element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : element.ToString();

        return true;
    }
}
