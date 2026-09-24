using DigitalDevServices.Services;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class TableColumnSortStateTests
{
    [TestMethod]
    public void CycleColumn_CyclesAscendingDescendingThenClears()
    {
        var sort = new TableColumnSortState();

        sort.CycleColumn("name");
        Assert.AreEqual("name", sort.ActiveColumn);
        Assert.IsFalse(sort.IsDescending);
        Assert.AreEqual("▲", sort.IndicatorFor("name"));

        sort.CycleColumn("name");
        Assert.AreEqual("name", sort.ActiveColumn);
        Assert.IsTrue(sort.IsDescending);
        Assert.AreEqual("▼", sort.IndicatorFor("name"));

        sort.CycleColumn("name");
        Assert.IsNull(sort.ActiveColumn);
        Assert.IsFalse(sort.IsDescending);
        Assert.AreEqual(string.Empty, sort.IndicatorFor("name"));
    }

    [TestMethod]
    public void Apply_SortsWhenColumnActive()
    {
        var sort = new TableColumnSortState();
        sort.CycleColumn("value");

        var items = new[] { new Item("b", 2), new Item("a", 1) };
        var columns = new Dictionary<string, Func<Item, IComparable>>
        {
            ["value"] = item => item.Value
        };

        var sorted = TableColumnSort.Apply(items, sort, columns);

        Assert.AreEqual(1, sorted[0].Value);
        Assert.AreEqual(2, sorted[1].Value);
    }

    private sealed record Item(string Name, int Value);
}
