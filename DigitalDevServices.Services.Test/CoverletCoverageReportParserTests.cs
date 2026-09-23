using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageReportParserTests
{
    private static readonly ICoverletCoverageReportParser Parser = new CoverletCoverageReportParser();

    [TestMethod]
    public void Parse_ReturnsMethodRowsFromSampleJson()
    {
        var json = File.ReadAllText(GetSamplePath("coverage.sample.json"));

        var result = Parser.Parse(json);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Rows);
        Assert.AreEqual("SampleAssembly.dll", result.Rows[0].Module);
        Assert.AreEqual("Sample.Namespace.SampleClass", result.Rows[0].ClassName);
        Assert.AreEqual("DoWork()", result.Rows[0].MethodName);
        Assert.AreEqual(80.0m, result.Rows[0].LineCoveragePercent);
        Assert.IsNotNull(result.Summary);
        Assert.AreEqual(2, result.Summary!.TotalMethods);
    }

    [TestMethod]
    public void Parse_ReturnsMethodRowsFromLegacySampleJson()
    {
        var json = File.ReadAllText(GetSamplePath("coverage.legacy.sample.json"));

        var result = Parser.Parse(json);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Rows);
        Assert.AreEqual("SampleClass.cs", result.Rows[0].SourceFile);
        Assert.AreEqual("Sample.Namespace.SampleClass::DoWork()", result.Rows[0].MethodName);
        Assert.AreEqual(50.0m, result.Rows[0].LineCoveragePercent);
        Assert.AreEqual(1, result.Rows[0].CoveredBranches);
        Assert.AreEqual(2, result.Rows[0].TotalBranches);
    }

    [TestMethod]
    public void Parse_RejectsEmptyContent()
    {
        var result = Parser.Parse("   ");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "empty");
    }

    [TestMethod]
    public void Parse_RejectsInvalidJson()
    {
        var result = Parser.Parse("{ not json");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "Invalid JSON");
    }

    [TestMethod]
    public void Parse_RejectsUnrecognizedSchema()
    {
        var result = Parser.Parse("""{ "only": "metadata" }""");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "no coverage modules");
    }

    private static string GetSamplePath(string fileName)
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "coverlet"));
        return Path.Combine(directory, fileName);
    }
}
