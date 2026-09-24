using DigitalDevServices.Model.Coverlet;
using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageModuleHideTests
{
    [TestMethod]
    public void HideMethodsInModules_AddsAllMethodRowKeysForSelectedModules()
    {
        var rows = new[]
        {
            CreateRow("a1", "Alpha.dll"),
            CreateRow("a2", "Alpha.dll"),
            CreateRow("b1", "Beta.dll")
        };

        var hidden = new HashSet<string>(StringComparer.Ordinal) { "existing" };
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha.dll" };

        var result = CoverletCoverageModuleHide.HideMethodsInModules(rows, hidden, modules);

        Assert.IsTrue(result.Contains("existing"));
        Assert.IsTrue(result.Contains("a1"));
        Assert.IsTrue(result.Contains("a2"));
        Assert.IsFalse(result.Contains("b1"));
    }

    private static CoverletCoverageRow CreateRow(string rowKey, string module) =>
        new()
        {
            RowKey = rowKey,
            Module = module,
            ClassName = "C",
            MethodName = "M"
        };
}
