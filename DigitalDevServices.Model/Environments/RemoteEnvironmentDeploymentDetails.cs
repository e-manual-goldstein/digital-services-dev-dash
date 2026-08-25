using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalDevServices.Model.Environments;

public class RemoteEnvironmentDeploymentDetails
{
    public EnvironmentBuild[] Builds { get; set; } = [];

    public EnvironmentBuild[] BuildsFull { get; set; } = [];

    public EnvironmentBuild[] BuildsLast { get; set; } = [];

    public EnvironmentBuild[] BuildsSuccessful { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    public EnvironmentBuild? GetPrimaryBuild()
    {
        return FirstNonEmpty(BuildsSuccessful)
            ?? FirstNonEmpty(BuildsLast)
            ?? FirstNonEmpty(BuildsFull)
            ?? FirstNonEmpty(Builds);
    }

    public string? GetPrimaryWipBranch() => GetPrimaryBuild()?.TryGetParameterValue("WipBranch");

    private static EnvironmentBuild? FirstNonEmpty(EnvironmentBuild[] builds) =>
        builds.Length > 0 ? builds[0] : null;
}
