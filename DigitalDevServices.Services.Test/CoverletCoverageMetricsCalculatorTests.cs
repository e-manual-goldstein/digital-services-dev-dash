using System.Text.Json;
using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageMetricsCalculatorTests
{
    [TestMethod]
    public void FromLines_CountsCoveredAndCoverableLines()
    {
        using var document = JsonDocument.Parse("""
            {
              "10": 1,
              "11": 0,
              "12": 2
            }
            """);

        var metrics = CoverletCoverageMetricsCalculator.FromLines(document.RootElement);

        Assert.AreEqual(2, metrics.CoveredLines);
        Assert.AreEqual(3, metrics.CoverableLines);
        Assert.AreEqual(66.67m, metrics.LineCoveragePercent);
    }

    [TestMethod]
    public void FromBranches_CountsBranchHits()
    {
        using var document = JsonDocument.Parse("""
            {
              "10": [0, 1, 0]
            }
            """);

        var metrics = CoverletCoverageMetricsCalculator.FromBranches(document.RootElement);

        Assert.AreEqual(1, metrics.CoveredBranches);
        Assert.AreEqual(3, metrics.TotalBranches);
        Assert.AreEqual(33.33m, metrics.BranchCoveragePercent);
    }
}
