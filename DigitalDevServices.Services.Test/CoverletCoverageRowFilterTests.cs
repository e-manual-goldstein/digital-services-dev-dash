using DigitalDevServices.Model.Coverlet;
using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageRowFilterTests
{
    [TestMethod]
    public void Apply_CombinesFiltersWithAnd()
    {
        var rows = new[]
        {
            CreateRow("A.dll", "Ns.One", "Run", lineCoverage: 80),
            CreateRow("A.dll", "Ns.Two", "Run", lineCoverage: 40),
            CreateRow("B.dll", "Ns.One", "Run", lineCoverage: 90)
        };

        var filtered = CoverletCoverageRowFilter.Apply(rows, new CoverletCoverageFilterState
        {
            ModuleContains = "A.dll",
            MinLineCoveragePercent = 50
        });

        Assert.HasCount(1, filtered);
        Assert.AreEqual("Ns.One", filtered[0].ClassName);
    }

    private static CoverletCoverageRow CreateRow(
        string module,
        string className,
        string method,
        decimal lineCoverage) =>
        new()
        {
            RowKey = $"{module}|{className}|{method}",
            Module = module,
            ClassName = className,
            MethodName = method,
            LineCoveragePercent = lineCoverage
        };
}
