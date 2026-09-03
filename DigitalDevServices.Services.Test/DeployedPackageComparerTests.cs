using DigitalDevServices.Model.Applications;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class DeployedPackageComparerTests
{
    [TestMethod]
    public void Compare_HighlightsVersionMismatch()
    {
        var left = CreateScan("Common.dll", assemblyVersion: "1.0.0.0");
        var right = CreateScan("Common.dll", assemblyVersion: "2.0.0.0");

        var rows = DeployedPackageComparer.Compare(left, right);

        Assert.HasCount(1, rows);
        Assert.AreEqual(DeployedPackageComparisonStatus.Mismatch, rows[0].Status);
        Assert.AreEqual("1.0.0.0", rows[0].LeftVersion);
        Assert.AreEqual("2.0.0.0", rows[0].RightVersion);
    }

    [TestMethod]
    public void Compare_HighlightsPackagesPresentOnOnlyOneSide()
    {
        var left = CreateScan(
            ("Shared.dll", "1.0.0.0"),
            ("LeftOnly.dll", "1.0.0.0"));
        var right = CreateScan(
            ("Shared.dll", "1.0.0.0"),
            ("RightOnly.dll", "3.0.0.0"));

        var rows = DeployedPackageComparer.Compare(left, right).ToDictionary(row => row.FileName);

        Assert.AreEqual(DeployedPackageComparisonStatus.Match, rows["Shared.dll"].Status);
        Assert.AreEqual(DeployedPackageComparisonStatus.LeftOnly, rows["LeftOnly.dll"].Status);
        Assert.AreEqual(DeployedPackageComparisonStatus.RightOnly, rows["RightOnly.dll"].Status);
    }

    private static DeployedPackageScanResult CreateScan(params (string FileName, string Version)[] packages) =>
        CreateScan(packages.Select(package => (package.FileName, package.Version, (string?)null)).ToArray());

    private static DeployedPackageScanResult CreateScan(
        string fileName,
        string? assemblyVersion = null,
        string? fileVersion = null) =>
        CreateScan([(fileName, assemblyVersion, fileVersion)]);

    private static DeployedPackageScanResult CreateScan(
        IReadOnlyList<(string FileName, string? AssemblyVersion, string? FileVersion)> packages) =>
        new()
        {
            Packages = packages
                .Select(package => new DeployedPackageInfo
                {
                    FileName = package.FileName,
                    AssemblyVersion = package.AssemblyVersion,
                    FileVersion = package.FileVersion
                })
                .ToList()
        };
}
