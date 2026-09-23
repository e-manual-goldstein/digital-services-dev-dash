using DigitalDevServices.Model.Coverlet;
using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageSummaryBuilderTests
{
    [TestMethod]
    public void BuildModuleSummaries_ExcludesHiddenRowsAndAggregatesLineCoverage()
    {
        var rows = new[]
        {
            CreateMethodRow("key-a1", "A.dll", "ClassOne", covered: 8, coverable: 10),
            CreateMethodRow("key-a2", "A.dll", "ClassTwo", covered: 0, coverable: 10),
            CreateMethodRow("key-b1", "B.dll", "ClassOne", covered: 5, coverable: 5),
            CreateMethodRow("key-hide", "B.dll", "Hidden", covered: 0, coverable: 100)
        };

        var hidden = new HashSet<string>(StringComparer.Ordinal) { "key-hide" };
        var summaries = CoverletCoverageSummaryBuilder.BuildModuleSummaries(rows, hidden);

        Assert.HasCount(2, summaries);

        var moduleA = summaries.Single(row => row.ModuleName == "A.dll");
        Assert.AreEqual(2, moduleA.ClassCount);
        Assert.AreEqual(2, moduleA.MethodCount);
        Assert.AreEqual(40m, moduleA.LineCoveragePercent);

        var moduleB = summaries.Single(row => row.ModuleName == "B.dll");
        Assert.AreEqual(1, moduleB.ClassCount);
        Assert.AreEqual(1, moduleB.MethodCount);
        Assert.AreEqual(100m, moduleB.LineCoveragePercent);
    }

    [TestMethod]
    public void BuildClassSummaries_FiltersByModuleAndExcludesHiddenRows()
    {
        var rows = new[]
        {
            CreateMethodRow("m1", "A.dll", "Alpha", covered: 1, coverable: 2),
            CreateMethodRow("m2", "A.dll", "Alpha", covered: 3, coverable: 4),
            CreateMethodRow("m3", "A.dll", "Beta", covered: 0, coverable: 4),
            CreateMethodRow("m4", "B.dll", "Other", covered: 4, coverable: 4),
            CreateMethodRow("hidden", "A.dll", "Alpha", covered: 0, coverable: 10)
        };

        var hidden = new HashSet<string>(StringComparer.Ordinal) { "hidden" };
        var summaries = CoverletCoverageSummaryBuilder.BuildClassSummaries(rows, hidden, "A.dll");

        Assert.HasCount(2, summaries);

        var alpha = summaries.Single(row => row.ClassName == "Alpha");
        Assert.AreEqual(2, alpha.MethodCount);
        Assert.AreEqual(66.67m, alpha.LineCoveragePercent);

        var beta = summaries.Single(row => row.ClassName == "Beta");
        Assert.AreEqual(1, beta.MethodCount);
        Assert.AreEqual(0m, beta.LineCoveragePercent);
    }

    private static CoverletCoverageRow CreateMethodRow(
        string rowKey,
        string module,
        string className,
        int covered,
        int coverable) =>
        new()
        {
            RowKey = rowKey,
            Module = module,
            ClassName = className,
            MethodName = "Method",
            CoveredLines = covered,
            CoverableLines = coverable,
            TotalLines = coverable,
            LineCoveragePercent = coverable == 0 ? 0 : Math.Round(covered * 100m / coverable, 2)
        };
}
