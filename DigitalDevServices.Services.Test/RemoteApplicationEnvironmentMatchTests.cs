using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class RemoteApplicationEnvironmentMatchTests
{
    [TestMethod]
    public void Find_WindowsServiceMatch_GetMachineName_UsesServiceMachineNameWithoutEnvironmentUrl()
    {
        var details = new RemoteEnvironmentDetails
        {
            WindowsServices =
            [
                new EnvironmentWindowsService
                {
                    MachineName = "UAT-01-APP",
                    DisplayName = "Digital Services Worker",
                    BinaryPathName = @"C:\Services\DigitalServices.Worker.exe"
                }
            ]
        };

        var match = RemoteApplicationEnvironmentMatch.Find(details, "Digital Services Worker");

        Assert.AreEqual("UAT-01-APP", match.GetMachineName(details));
    }

    [TestMethod]
    public void Find_WindowsServiceMatch_GetMachineName_FallsBackToWebSiteWhenServiceMachineNameMissing()
    {
        var details = new RemoteEnvironmentDetails
        {
            WebSites =
            [
                new EnvironmentWebSite
                {
                    Name = "Default Web Site",
                    MachineName = "UAT-01-APP"
                }
            ],
            WindowsServices =
            [
                new EnvironmentWindowsService
                {
                    DisplayName = "Digital Services Worker",
                    BinaryPathName = @"C:\Services\DigitalServices.Worker.exe"
                }
            ]
        };

        var match = RemoteApplicationEnvironmentMatch.Find(details, "Digital Services Worker");

        Assert.AreEqual("UAT-01-APP", match.GetMachineName(details));
    }

    [TestMethod]
    public void Find_WindowsServiceMatch_GetTemplateContextWebSite_ReturnsWebSiteWithoutEnvironmentUrl()
    {
        var webSite = new EnvironmentWebSite
        {
            Name = "Default Web Site",
            MachineName = "UAT-01-APP"
        };
        var details = new RemoteEnvironmentDetails
        {
            WebSites = [webSite],
            WindowsServices =
            [
                new EnvironmentWindowsService
                {
                    DisplayName = "Digital Services Worker",
                    BinaryPathName = @"C:\Services\DigitalServices.Worker.exe"
                }
            ]
        };

        var match = RemoteApplicationEnvironmentMatch.Find(details, "Digital Services Worker");

        Assert.AreSame(webSite, match.GetTemplateContextWebSite(details));
    }
}
