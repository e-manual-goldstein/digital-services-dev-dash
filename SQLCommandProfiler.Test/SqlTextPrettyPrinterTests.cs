namespace SQLCommandProfiler.Test;

[TestClass]
public sealed class SqlTextPrettyPrinterTests
{
    [TestMethod]
    public void FormatOrOriginal_AddsLineBreaks_ForSimpleSelect()
    {
        const string sql = "SELECT a, b FROM dbo.Orders WHERE Id = @p0";

        var formatted = SqlTextPrettyPrinter.FormatOrOriginal(sql);

        StringAssert.Contains(formatted, "SELECT");
        StringAssert.Contains(formatted, "FROM");
        Assert.IsTrue(formatted.Contains('\n') || formatted.Contains('\r'));
    }

    [TestMethod]
    public void FormatOrOriginal_ReturnsOriginal_WhenParseFails()
    {
        const string sql = "NOT VALID SQL {{{";

        var formatted = SqlTextPrettyPrinter.FormatOrOriginal(sql);

        Assert.AreEqual(sql, formatted);
    }
}
