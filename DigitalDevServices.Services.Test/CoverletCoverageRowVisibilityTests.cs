using DigitalDevServices.Model.Coverlet;
using DigitalDevServices.Services.Coverlet;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CoverletCoverageRowVisibilityTests
{
    [TestMethod]
    public void Apply_OmitsHiddenRowsUnlessShowHidden()
    {
        var rows = new[]
        {
            CreateRow("a"),
            CreateRow("b")
        };
        var hidden = new HashSet<string>(StringComparer.Ordinal) { "a" };

        var visible = CoverletCoverageRowVisibility.Apply(rows, hidden, showHidden: false);
        var includingHidden = CoverletCoverageRowVisibility.Apply(rows, hidden, showHidden: true);

        Assert.HasCount(1, visible);
        Assert.AreEqual("b", visible[0].RowKey);
        Assert.HasCount(2, includingHidden);
    }

    private static CoverletCoverageRow CreateRow(string key) =>
        new()
        {
            RowKey = key,
            Module = "M",
            ClassName = "C",
            MethodName = "M"
        };
}
