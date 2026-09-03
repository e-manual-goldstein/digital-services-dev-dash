using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

internal static class EnvironmentRefreshSnapshotCollector
{
    public static IReadOnlyCollection<string> CollectPipelineBuildNumbers(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        IReadOnlyList<ApplicationInstance> instances)
    {
        var buildNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (deploymentDetails is null)
        {
            return buildNumbers;
        }

        var primaryBuild = deploymentDetails.GetPrimaryBuild();
        if (primaryBuild is not null)
        {
            buildNumbers.Add(primaryBuild.EnvironmentPipelineBuildNumber.ToString());
        }

        foreach (var instance in instances)
        {
            var build = deploymentDetails.GetBuildForApplication(instance.DeployableApplication.Name);
            if (build is not null)
            {
                buildNumbers.Add(build.EnvironmentPipelineBuildNumber.ToString());
            }
        }

        return buildNumbers;
    }

    public static EnvironmentRefreshSnapshot Create(
        RemoteEnvironmentDetails details,
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        IReadOnlyDictionary<string, RemoteBuildVersionDetails> buildVersionDetails,
        DateTimeOffset refreshedAt) =>
        new()
        {
            Details = details,
            DeploymentDetails = deploymentDetails,
            BuildVersionDetailsByBuildNumber = buildVersionDetails,
            DateLastRefreshed = refreshedAt
        };
}
