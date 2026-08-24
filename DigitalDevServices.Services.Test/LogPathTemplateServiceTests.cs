using DigitalDevServices.Model.Applications;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class LogPathTemplateServiceTests
{
    private readonly ILogPathTemplateService _service = new LogPathTemplateService();

    [TestMethod]
    public void Resolve_ReplacesKnownTokens()
    {
        var result = _service.Resolve(
            @"{MachineName}\{EnvironmentCode}\{AppName}\Logs",
            new LogPathTemplateContext
            {
                MachineName = "UAT-01-APP",
                EnvironmentCode = "UAT-01",
                AppName = "portal"
            });

        Assert.AreEqual(@"UAT-01-APP\UAT-01\portal\Logs", result.ResolvedPath);
        Assert.HasCount(0, result.UnknownTokens);
    }

    [TestMethod]
    public void Resolve_IsCaseInsensitiveForTokenNames()
    {
        var result = _service.Resolve(
            @"{machinename}\{environmentcode}\{appname}\Logs",
            new LogPathTemplateContext
            {
                MachineName = "HOST-01",
                EnvironmentCode = "INT",
                AppName = "api"
            });

        Assert.AreEqual(@"HOST-01\INT\api\Logs", result.ResolvedPath);
    }

    [TestMethod]
    public void Resolve_ReplacesAllDocumentedTokens()
    {
        var result = _service.Resolve(
            "{AppName}|{EnvironmentCode}|{EnvironmentName}|{MachineName}|{ApplicationPoolName}|{VirtualPath}|{PhysicalPath}",
            new LogPathTemplateContext
            {
                AppName = "portal",
                EnvironmentCode = "UAT-01",
                EnvironmentName = "UAT-01",
                MachineName = "UAT-01-APP",
                ApplicationPoolName = "PortalPool",
                VirtualPath = "/portal",
                PhysicalPath = @"C:\inetpub\portal"
            });

        Assert.AreEqual(
            @"portal|UAT-01|UAT-01|UAT-01-APP|PortalPool|/portal|C:\inetpub\portal",
            result.ResolvedPath);
    }

    [TestMethod]
    public void Resolve_LeavesUnknownTokensUnchanged()
    {
        var result = _service.Resolve(
            @"{MachineName}\{UnknownToken}\Logs",
            new LogPathTemplateContext
            {
                MachineName = "UAT-01-APP"
            });

        Assert.AreEqual(@"UAT-01-APP\{UnknownToken}\Logs", result.ResolvedPath);
        Assert.HasCount(1, result.UnknownTokens);
        Assert.AreEqual("UnknownToken", result.UnknownTokens[0]);
    }

    [TestMethod]
    public void Resolve_ReturnsEmptyForBlankTemplate()
    {
        var result = _service.Resolve("   ", new LogPathTemplateContext { AppName = "portal" });

        Assert.AreEqual(string.Empty, result.ResolvedPath);
        Assert.HasCount(0, result.UnknownTokens);
    }

    [TestMethod]
    public void Resolve_ReplacesMissingTokenValuesWithEmptyString()
    {
        var result = _service.Resolve(
            @"{MachineName}\{AppName}\Logs",
            new LogPathTemplateContext
            {
                MachineName = "UAT-01-APP"
            });

        Assert.AreEqual(@"UAT-01-APP\\Logs", result.ResolvedPath);
    }
}
