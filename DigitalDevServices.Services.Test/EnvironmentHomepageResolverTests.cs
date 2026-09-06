using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class EnvironmentHomepageResolverTests
{
    [TestMethod]
    public void SuggestHomepageUrl_MatchesEnvironmentUrlByApplicationName()
    {
        var details = new RemoteEnvironmentDetails
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
        };

        Assert.AreEqual(
            "https://uat-01.example.com/portal",
            EnvironmentHomepageResolver.SuggestHomepageUrl(details, "Customer Portal", isWebApp: true));
    }

    [TestMethod]
    public void SuggestHomepageUrl_ReturnsNullForNonWebApps()
    {
        var details = new RemoteEnvironmentDetails
        {
            Id = 1,
            Code = "UAT-01",
            Name = "UAT-01",
            EnvironmentType = "UAT",
            EnvironmentUrls =
            [
                new EnvironmentUrl
                {
                    ApplicationName = "Worker",
                    Url = "https://uat-01.example.com/worker"
                }
            ]
        };

        Assert.IsNull(EnvironmentHomepageResolver.SuggestHomepageUrl(details, "Worker", isWebApp: false));
    }

    [TestMethod]
    public void IsHomepageUrlManualOverride_DetectsChangedValue()
    {
        Assert.IsFalse(EnvironmentHomepageResolver.IsHomepageUrlManualOverride(
            "https://uat-01.example.com/portal",
            "https://uat-01.example.com/portal"));

        Assert.IsTrue(EnvironmentHomepageResolver.IsHomepageUrlManualOverride(
            "https://custom.example.com",
            "https://uat-01.example.com/portal"));
    }
}
