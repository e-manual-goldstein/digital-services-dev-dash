using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class DeploymentManifestParserTests
{
    [TestMethod]
    public void ParseLines_SkipsHeaderAndReadsQuotedFields()
    {
        var result = DeploymentManifestParser.ParseLines(
        [
            "\"Path\",\"Version\"",
            "\"bin\\Customer.Api.dll\",\"1.2.3.4\"",
            "\"lib\\Common.dll\",\"2.0.0.0\""
        ]);

        Assert.IsTrue(result.CouldReadFile);
        Assert.HasCount(2, result.Packages);
        var customerApi = result.Packages.Single(package => package.FileName == "Customer.Api.dll");
        Assert.AreEqual(@"bin\Customer.Api.dll", customerApi.RepresentativePath);
        Assert.AreEqual("1.2.3.4", customerApi.AssemblyVersion);
        var common = result.Packages.Single(package => package.FileName == "Common.dll");
        Assert.AreEqual(@"lib\Common.dll", common.RepresentativePath);
        Assert.AreEqual("2.0.0.0", common.AssemblyVersion);
    }

    [TestMethod]
    public void ParseLines_AddsWarningForMalformedRow()
    {
        var result = DeploymentManifestParser.ParseLines(
        [
            "\"Path\",\"Version\"",
            "\"Valid.dll\",\"1.0.0.0\"",
            "not,a,valid,row"
        ]);

        Assert.HasCount(1, result.Packages);
        Assert.HasCount(1, result.Warnings);
        StringAssert.Contains(result.Warnings[0], "Line 3");
    }

    [TestMethod]
    public void ParseLines_ParsesQuotedCommasInsidePath()
    {
        var result = DeploymentManifestParser.ParseLines(
        [
            "\"Path\",\"Version\"",
            "\"folder\\part,one.dll\",\"9.8.7\""
        ]);

        Assert.HasCount(1, result.Packages);
        Assert.AreEqual(@"folder\part,one.dll", result.Packages[0].RepresentativePath);
        Assert.AreEqual("9.8.7", result.Packages[0].AssemblyVersion);
    }
}
