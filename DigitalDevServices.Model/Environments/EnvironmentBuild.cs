using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalDevServices.Model.Environments;

public class EnvironmentBuild
{
    // (2) WorkItemBuildNumber — TFS work item id for this environment build; used for TFS hyperlinks and GetBuildVersionDetails.
    // Proposed rename: WorkItemBuildNumber
    // Correct Name: EnvironmentPipelineBuildNumber
    [JsonPropertyName("BuildNumber")]
    public int EnvironmentPipelineBuildNumber { get; set; }

    public string? Color { get; set; }

    public string? DeploymentType { get; set; }

    public string? Name { get; set; }

    public EnvironmentBuildParameter[] Parameters { get; set; } = [];

    public string? BuildVersionNumber => TryGetParameterValue("codeBuildNumber") ?? TryGetParameterValue("BuildVersion");

    public string? Result { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    public string? TryGetParameterValue(string parameterName, bool notNull = false)
    {
        if (Parameters.Length == 0)
        {
            return null;
        }

        var match = Parameters.FirstOrDefault(parameter =>
            string.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase));
        if (notNull && match?.Value is null)
        {
            throw new ArgumentNullException();
        }
        return string.IsNullOrWhiteSpace(match?.Value) ? null : match.Value.Trim();
    }
}
