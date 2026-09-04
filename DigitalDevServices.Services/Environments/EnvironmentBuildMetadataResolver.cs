using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public static class EnvironmentBuildMetadataResolver
{
    public static EnvironmentBuild? GetBuild(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string applicationName) =>
        deploymentDetails?.GetBuildForApplication(applicationName);

    public static string? SuggestBuildVersionNumber(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string applicationName)
    {
        var build = GetBuild(deploymentDetails, applicationName);
        if (build is null)
        {
            return null;
        }

        return build.BuildVersionNumber
            ?? (build.EnvironmentPipelineBuildNumber > 0
                ? build.EnvironmentPipelineBuildNumber.ToString()
                : null);
    }

    public static RemoteBuildVersionDetails? GetBuildVersionDetails(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string applicationName,
        IReadOnlyDictionary<string, RemoteBuildVersionDetails>? buildVersionDetailsByBuildNumber)
    {
        if (deploymentDetails is null || buildVersionDetailsByBuildNumber is null)
        {
            return null;
        }

        var pipelineBuildNumber = GetBuild(deploymentDetails, applicationName)?.EnvironmentPipelineBuildNumber.ToString();
        if (string.IsNullOrWhiteSpace(pipelineBuildNumber))
        {
            return null;
        }

        buildVersionDetailsByBuildNumber.TryGetValue(pipelineBuildNumber, out var details);
        return details;
    }

    public static string? SuggestSourceBranch(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string applicationName,
        IReadOnlyDictionary<string, RemoteBuildVersionDetails>? buildVersionDetailsByBuildNumber = null)
    {
        var buildVersionDetails = GetBuildVersionDetails(
            deploymentDetails,
            applicationName,
            buildVersionDetailsByBuildNumber);

        return buildVersionDetails?.SourceBranch
            ?? deploymentDetails?.GetWipBranchForApplication(applicationName)
            ?? deploymentDetails?.GetPrimaryWipBranch();
    }
}
