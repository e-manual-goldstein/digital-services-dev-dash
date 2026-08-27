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

    public EnvironmentBuild? GetBuildForApplication(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            return GetPrimaryBuild();
        }

        return FirstMatching(BuildsSuccessful, applicationName)
            ?? FirstMatching(BuildsLast, applicationName)
            ?? FirstMatching(BuildsFull, applicationName)
            ?? FirstMatching(Builds, applicationName)
            ?? GetPrimaryBuild();
    }

    public string? GetWipBranchForApplication(string applicationName) =>
        GetBuildForApplication(applicationName)?.TryGetParameterValue("WipBranch");

    public string? GetBuildNumberForApplication(string applicationName)
    {
        var build = GetBuildForApplication(applicationName);
        return build is null ? null : build.BuildVersionNumber;
    }

    private static EnvironmentBuild? FirstMatching(EnvironmentBuild[] builds, string applicationName) =>
        builds.FirstOrDefault(build =>
            string.Equals(build.Name, applicationName.Trim(), StringComparison.OrdinalIgnoreCase));

    private static EnvironmentBuild? FirstNonEmpty(EnvironmentBuild[] builds) =>
        builds.Length > 0 ? builds[0] : null;
}
