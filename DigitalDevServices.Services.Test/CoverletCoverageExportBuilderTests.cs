using DigitalDevServices.Model.Coverlet;
using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageExportBuilderTests
{
    [TestMethod]
    public void SelectRowsForExport_AppliesFiltersAndExcludesHidden()
    {
        var rows = new[]
        {
            CreateRow("keep", "A.dll", "C", "M1"),
            CreateRow("hide", "A.dll", "C", "M2"),
            CreateRow("other", "B.dll", "C", "M1")
        };

        var hidden = new HashSet<string>(StringComparer.Ordinal) { "hide" };
        var exportRows = CoverletCoverageExportBuilder.SelectRowsForExport(
            rows,
            new CoverletCoverageFilterState { ModuleContains = "A.dll" },
            hidden);

        Assert.HasCount(1, exportRows);
        Assert.AreEqual("keep", exportRows[0].RowKey);
    }

    [TestMethod]
    public void ToJson_IncludesMetadataAndRows()
    {
        var rows = new[] { CreateRow("k", "Mod.dll", "Cls", "Run()") };
        var json = CoverletCoverageExportBuilder.ToJson(rows, "coverage.json");

        Assert.Contains("\"sourceFileName\": \"coverage.json\"", json);
        Assert.Contains("\"rowCount\": 1", json);
        Assert.Contains("\"module\": \"Mod.dll\"", json);
        Assert.Contains("\"methodName\": \"Run()\"", json);
    }

    [TestMethod]
    public void BuildExportFileName_UsesFilteredSuffix()
    {
        Assert.AreEqual("run.filtered.json", CoverletCoverageExportBuilder.BuildExportFileName("run.json"));
        Assert.AreEqual("run.filtered.csv", CoverletCoverageExportBuilder.BuildExportFileName("run.json", "csv"));
        Assert.AreEqual("coverage-export.json", CoverletCoverageExportBuilder.BuildExportFileName(null));
        Assert.AreEqual("coverage-export.csv", CoverletCoverageExportBuilder.BuildExportFileName(null, "csv"));
    }

    [TestMethod]
    public void ToCsv_EscapesCommasAndQuotes()
    {
        var rows = new[]
        {
            new CoverletCoverageRow
            {
                RowKey = "k",
                Module = "A,B.dll",
                ClassName = "Ns.\"Type\"",
                MethodName = "Run()",
                CoveredLines = 1,
                CoverableLines = 2,
                LineCoveragePercent = 50m
            }
        };

        var csv = CoverletCoverageExportBuilder.ToCsv(rows);

        Assert.StartsWith(
            "Module,SourceFile,ClassName,MethodName,CoveredLines,CoverableLines,LineCoveragePercent,CoveredBranches,TotalBranches,BranchCoveragePercent",
            csv);
        Assert.Contains("\"A,B.dll\"", csv);
        Assert.Contains("\"Ns.\"\"Type\"\"\"", csv);
    }

    private static CoverletCoverageRow CreateRow(string key, string module, string className, string method) =>
        new()
        {
            RowKey = key,
            Module = module,
            ClassName = className,
            MethodName = method
        };
}
