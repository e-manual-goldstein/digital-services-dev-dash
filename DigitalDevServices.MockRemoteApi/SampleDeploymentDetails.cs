using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.MockRemoteApi;

internal static class SampleDeploymentDetails
{
    private static readonly EnvironmentBuild CustomerPortalBuild = new()
    {
        BuildNumber = 123456,
        Color = "green",
        DeploymentType = "Full",
        Name = "Customer Portal",
        Parameters =
        [
            new EnvironmentBuildParameter
            {
                Name = "WipBranch",
                NameAsLabel = "WIP branch",
                Value = "feature/123456-customer-portal"
            }
        ],
        Result = "Succeeded"
    };

    public static RemoteEnvironmentDeploymentDetails? ForEnvironmentCode(string environmentCode)
    {
        if (!environmentCode.Equals("UAT-01", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new RemoteEnvironmentDeploymentDetails
        {
            Builds = [CustomerPortalBuild],
            BuildsFull = [CustomerPortalBuild],
            BuildsLast = [CustomerPortalBuild],
            BuildsSuccessful = [CustomerPortalBuild]
        };
    }
}
