using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalDevServices.Model.Environments;

public class EnvironmentBuild
{
    public int BuildNumber { get; set; }

    public string? Color { get; set; }

    public string? DeploymentType { get; set; }

    public string? Name { get; set; }

    public EnvironmentBuildParameter[] Parameters { get; set; } = [];

    public string? Result { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    public string? TryGetParameterValue(string parameterName)
    {
        if (Parameters.Length == 0)
        {
            return null;
        }

        var match = Parameters.FirstOrDefault(parameter =>
            string.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(match?.Value) ? null : match.Value.Trim();
    }
}
