using DigitalDevServices.Model.Coverlet;
using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageSummaryCalculatorTests
{
    [TestMethod]
    public void BuildFromRows_AggregatesLineAndBranchMetrics()
    {
        var rows = new[]
        {
            CreateRow(coveredLines: 2, coverableLines: 4, coveredBranches: 1, totalBranches: 2),
            CreateRow(coveredLines: 0, coverableLines: 6, coveredBranches: 0, totalBranches: 4)
        };

        var summary = CoverletCoverageSummaryCalculator.BuildFromRows(rows);

        Assert.AreEqual(2, summary.CoveredLines);
        Assert.AreEqual(10, summary.CoverableLines);
        Assert.AreEqual(20m, summary.LineCoveragePercent);
        Assert.AreEqual(1, summary.CoveredBranches);
        Assert.AreEqual(6, summary.TotalBranches);
        Assert.AreEqual(16.67m, summary.BranchCoveragePercent);
        Assert.AreEqual(2, summary.TotalMethods);
        Assert.AreEqual(1, summary.CoveredMethods);
    }

    [TestMethod]
    public void BuildFromRows_EmptyList_ReturnsZeros()
    {
        var summary = CoverletCoverageSummaryCalculator.BuildFromRows([]);

        Assert.AreEqual(0, summary.TotalMethods);
        Assert.AreEqual(0m, summary.LineCoveragePercent);
    }

    private static CoverletCoverageRow CreateRow(
        int coveredLines,
        int coverableLines,
        int coveredBranches,
        int totalBranches) =>
        new()
        {
            RowKey = Guid.NewGuid().ToString("N"),
            Module = "M",
            ClassName = "C",
            MethodName = "Run",
            CoveredLines = coveredLines,
            CoverableLines = coverableLines,
            TotalLines = coverableLines,
            LineCoveragePercent = coverableLines == 0 || coveredLines == 0 ? 0 : 100m,
            CoveredBranches = coveredBranches,
            TotalBranches = totalBranches,
            BranchCoveragePercent = 50m
        };
}
