using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class EnvironmentBuildMetadataResolverTests
{
    [TestMethod]
    public void SuggestBuildVersionNumber_FallsBackToPipelineBuildNumber()
    {
        var deploymentDetails = new RemoteEnvironmentDeploymentDetails
        {
            BuildsSuccessful =
            [
                new EnvironmentBuild
                {
                    EnvironmentPipelineBuildNumber = 123456,
                    Name = "Customer Portal"
                }
            ]
        };

        Assert.AreEqual(
            "123456",
            EnvironmentBuildMetadataResolver.SuggestBuildVersionNumber(deploymentDetails, "Customer Portal"));
    }

    [TestMethod]
    public void SuggestSourceBranch_PrefersBuildVersionDetailsOverWipBranch()
    {
        var deploymentDetails = new RemoteEnvironmentDeploymentDetails
        {
            BuildsSuccessful =
            [
                new EnvironmentBuild
                {
                    EnvironmentPipelineBuildNumber = 123456,
                    Name = "Customer Portal",
                    Parameters =
                    [
                        new EnvironmentBuildParameter
                        {
                            Name = "WipBranch",
                            Value = "feature/123456-portal"
                        }
                    ]
                }
            ]
        };

        var buildVersionDetails = new Dictionary<string, RemoteBuildVersionDetails>(StringComparer.OrdinalIgnoreCase)
        {
            ["123456"] = new RemoteBuildVersionDetails
            {
                BuildNumber = "123456",
                SourceBranch = "feature/123456-customer-portal"
            }
        };

        Assert.AreEqual(
            "feature/123456-customer-portal",
            EnvironmentBuildMetadataResolver.SuggestSourceBranch(
                deploymentDetails,
                "Customer Portal",
                buildVersionDetails));
    }
}
