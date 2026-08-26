using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class RemoteEnvironmentDeploymentDetailsTests
{
    [TestMethod]
    public void GetPrimaryBuild_PrefersBuildsSuccessfulThenBuildsLastThenBuildsFullThenBuilds()
    {
        var successful = new EnvironmentBuild { EnvironmentPipelineBuildNumber = 1, Name = "successful" };
        var last = new EnvironmentBuild { EnvironmentPipelineBuildNumber = 2, Name = "last" };
        var full = new EnvironmentBuild { EnvironmentPipelineBuildNumber = 3, Name = "full" };
        var builds = new EnvironmentBuild { EnvironmentPipelineBuildNumber = 4, Name = "builds" };

        var withSuccessful = new RemoteEnvironmentDeploymentDetails
        {
            Builds = [builds],
            BuildsFull = [full],
            BuildsLast = [last],
            BuildsSuccessful = [successful]
        };
        Assert.AreEqual(1, withSuccessful.GetPrimaryBuild()!.EnvironmentPipelineBuildNumber);

        var withLast = new RemoteEnvironmentDeploymentDetails
        {
            Builds = [builds],
            BuildsFull = [full],
            BuildsLast = [last]
        };
        Assert.AreEqual(2, withLast.GetPrimaryBuild()!.EnvironmentPipelineBuildNumber);

        var withFull = new RemoteEnvironmentDeploymentDetails
        {
            Builds = [builds],
            BuildsFull = [full]
        };
        Assert.AreEqual(3, withFull.GetPrimaryBuild()!.EnvironmentPipelineBuildNumber);

        var withBuildsOnly = new RemoteEnvironmentDeploymentDetails
        {
            Builds = [builds]
        };
        Assert.AreEqual(4, withBuildsOnly.GetPrimaryBuild()!.EnvironmentPipelineBuildNumber);
    }

    [TestMethod]
    public void GetBuildForApplication_PrefersNameMatchThenFallsBackToPrimaryBuild()
    {
        var primary = new EnvironmentBuild { EnvironmentPipelineBuildNumber = 99, Name = "Other" };
        var matched = new EnvironmentBuild
        {
            EnvironmentPipelineBuildNumber = 123456,
            Name = "Customer Portal",
            Parameters =
            [
                new EnvironmentBuildParameter
                {
                    Name = "WipBranch",
                    Value = "feature/123456-customer-portal"
                }
            ]
        };

        var details = new RemoteEnvironmentDeploymentDetails
        {
            BuildsSuccessful = [primary],
            BuildsLast = [matched]
        };

        Assert.AreEqual(123456, details.GetBuildForApplication("Customer Portal")!.EnvironmentPipelineBuildNumber);
        Assert.AreEqual(99, details.GetBuildForApplication("Missing")!.EnvironmentPipelineBuildNumber);
        Assert.AreEqual("123456", details.GetBuildNumberForApplication("Customer Portal"));
        Assert.AreEqual("feature/123456-customer-portal", details.GetWipBranchForApplication("Customer Portal"));
    }

    [TestMethod]
    public void GetPrimaryBuild_ReturnsNullWhenAllCollectionsEmpty()
    {
        var details = new RemoteEnvironmentDeploymentDetails();
        Assert.IsNull(details.GetPrimaryBuild());
        Assert.IsNull(details.GetPrimaryWipBranch());
    }

    [TestMethod]
    public void GetPrimaryWipBranch_ReadsWipBranchParameterFromPrimaryBuild()
    {
        var details = new RemoteEnvironmentDeploymentDetails
        {
            BuildsSuccessful =
            [
                new EnvironmentBuild
                {
                    EnvironmentPipelineBuildNumber = 99,
                    Parameters =
                    [
                        new EnvironmentBuildParameter
                        {
                            Name = "WipBranch",
                            NameAsLabel = "WIP branch",
                            Value = "feature/99"
                        }
                    ]
                }
            ]
        };

        Assert.AreEqual("feature/99", details.GetPrimaryWipBranch());
    }
}
