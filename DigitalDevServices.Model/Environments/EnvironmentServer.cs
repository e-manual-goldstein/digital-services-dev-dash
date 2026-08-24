using System.Text.Json.Serialization;

namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Infrastructure server returned as part of remote environment details.
/// </summary>
public class EnvironmentServer
{
    public string? ComponentDescription { get; set; }

    public string? ComponentIdenifier { get; set; }

    public string? ComponentName { get; set; }

    public string? ComponentResourceNameResolved { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    public string? ServerType { get; set; }
}
