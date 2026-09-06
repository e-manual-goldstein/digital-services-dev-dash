using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class EnvironmentInstanceSnapshotSyncServiceTests
{
    [TestMethod]
    public void ApplySnapshotToInstance_UpdatesBuildBranchSqlAndDeployedDate()
    {
        var instance = new ApplicationInstance
        {
            Id = Guid.NewGuid(),
            DeployableApplicationId = Guid.NewGuid(),
            EnvironmentId = Guid.NewGuid(),
            BuildVersionNumber = "old-build",
            DeployableApplication = new DeployableApplication
            {
                Id = Guid.NewGuid(),
                Name = "Customer Portal",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var refreshedAt = new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);
        var snapshot = new EnvironmentRefreshSnapshot
        {
            DateLastRefreshed = refreshedAt,
            Details = new RemoteEnvironmentDetails
            {
                Id = 1,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                AdditionalProperties = new Dictionary<string, System.Text.Json.JsonElement>
                {
                    ["SqlServerInstance"] = System.Text.Json.JsonSerializer.SerializeToElement(@"UAT-01\SQL2019")
                }
            },
            DeploymentDetails = new RemoteEnvironmentDeploymentDetails
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
                                Name = "codeBuildNumber",
                                Value = "260903.123456.0"
                            },
                            new EnvironmentBuildParameter
                            {
                                Name = "DeployedDate",
                                Value = "2026-09-01T08:30:00Z"
                            }
                        ]
                    }
                ]
            },
            BuildVersionDetailsByBuildNumber = new Dictionary<string, RemoteBuildVersionDetails>(StringComparer.OrdinalIgnoreCase)
            {
                ["123456"] = new RemoteBuildVersionDetails
                {
                    BuildNumber = "123456",
                    SourceBranch = "feature/123456-customer-portal"
                }
            }
        };

        snapshot.Details.TryGetAdditionalString("SqlServerInstance", out var sqlServerInstance);

        var changed = EnvironmentInstanceSnapshotSyncService.ApplySnapshotToInstance(
            instance,
            snapshot,
            sqlServerInstance);

        Assert.IsTrue(changed);
        Assert.AreEqual("260903.123456.0", instance.BuildVersionNumber);
        Assert.AreEqual("feature/123456-customer-portal", instance.SourceBranch);
        Assert.AreEqual(@"UAT-01\SQL2019", instance.SqlServerInstance);
        Assert.AreEqual(new DateTimeOffset(2026, 9, 1, 8, 30, 0, TimeSpan.Zero), instance.DeployedAt);
    }

    [TestMethod]
    public void ApplySnapshotToInstance_UpdatesHomepageUrlForWebApps()
    {
        var instance = new ApplicationInstance
        {
            Id = Guid.NewGuid(),
            DeployableApplicationId = Guid.NewGuid(),
            EnvironmentId = Guid.NewGuid(),
            BuildVersionNumber = "123456",
            DeployableApplication = new DeployableApplication
            {
                Id = Guid.NewGuid(),
                Name = "Customer Portal",
                IsWebApp = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var snapshot = new EnvironmentRefreshSnapshot
        {
            DateLastRefreshed = DateTimeOffset.UtcNow,
            Details = new RemoteEnvironmentDetails
            {
                Id = 1,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                EnvironmentUrls =
                [
                    new EnvironmentUrl
                    {
                        ApplicationName = "Customer Portal",
                        Url = "https://uat-01.example.com/portal"
                    }
                ]
            }
        };

        var changed = EnvironmentInstanceSnapshotSyncService.ApplySnapshotToInstance(
            instance,
            snapshot,
            environmentSqlServerInstance: null);

        Assert.IsTrue(changed);
        Assert.AreEqual("https://uat-01.example.com/portal", instance.HomepageUrl);
    }

    [TestMethod]
    public void ApplySnapshotToInstance_SkipsHomepageUrlWhenManualOverrideSet()
    {
        var instance = new ApplicationInstance
        {
            Id = Guid.NewGuid(),
            DeployableApplicationId = Guid.NewGuid(),
            EnvironmentId = Guid.NewGuid(),
            BuildVersionNumber = "123456",
            HomepageUrl = "https://custom.example.com",
            HomepageUrlIsManual = true,
            DeployableApplication = new DeployableApplication
            {
                Id = Guid.NewGuid(),
                Name = "Customer Portal",
                IsWebApp = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var snapshot = new EnvironmentRefreshSnapshot
        {
            DateLastRefreshed = DateTimeOffset.UtcNow,
            Details = new RemoteEnvironmentDetails
            {
                Id = 1,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                EnvironmentUrls =
                [
                    new EnvironmentUrl
                    {
                        ApplicationName = "Customer Portal",
                        Url = "https://uat-01.example.com/portal"
                    }
                ]
            }
        };

        var changed = EnvironmentInstanceSnapshotSyncService.ApplySnapshotToInstance(
            instance,
            snapshot,
            environmentSqlServerInstance: null);

        Assert.IsFalse(changed);
        Assert.AreEqual("https://custom.example.com", instance.HomepageUrl);
    }
}
