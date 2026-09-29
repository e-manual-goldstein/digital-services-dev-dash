namespace SQLCommandProfiler.Test;

[TestClass]
public sealed class EfSqlInterpreterTests
{
    private readonly EfSqlInterpreter _interpreter = new();

    [TestMethod]
    public void Interpret_ReturnsNull_ForNonEfSql()
    {
        var result = _interpreter.Interpret("SELECT 1 AS Value");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Interpret_ClassifiesEf6Select_AsReadOnly_WithTables()
    {
        const string sql = """
            SELECT
                [Extent1].[Id] AS [Id],
                [Extent1].[Name] AS [Name]
            FROM [dbo].[Customers] AS [Extent1]
            WHERE [Extent1].[Id] = @p0
            """;

        var result = _interpreter.Interpret(sql);
        Assert.IsNotNull(result);
        Assert.IsTrue(result!.IsLikelyEf6);
        Assert.AreEqual(EfSqlAccess.ReadOnly, result.Access);

        var select = result.Statements.Single(statement => statement.Kind == EfSqlStatementKind.Select);
        CollectionAssert.AreEquivalent(
            new[] { "dbo.Customers" },
            select.TablesRead.Select(table => $"{table.Schema}.{table.Name}").ToArray());
    }

    [TestMethod]
    public void Interpret_ClassifiesEf6Insert_AsReadWrite_WithColumns()
    {
        const string sql = """
            INSERT [dbo].[Orders]([CustomerId], [OrderDate])
            VALUES (@p0, @p1)
            """;

        var result = _interpreter.Interpret(sql);
        Assert.IsNotNull(result);
        Assert.AreEqual(EfSqlAccess.ReadWrite, result!.Access);

        var insert = result.Statements.Single(statement => statement.Kind == EfSqlStatementKind.Insert);
        Assert.AreEqual("Orders", insert.Insert!.Table.Name);
        CollectionAssert.AreEqual(new[] { "CustomerId", "OrderDate" }, insert.Insert.Columns.ToArray());
        CollectionAssert.AreEqual(new[] { "@p0", "@p1" }, insert.Insert.ValueExpressions.ToArray());
    }

    [TestMethod]
    public void Interpret_ClassifiesEf6Insert_WithExtentMarker_AsReadWrite()
    {
        const string sql = """
            INSERT [dbo].[Orders]([CustomerId], [OrderDate])
            SELECT @p0, @p1
            FROM [dbo].[Customers] AS [Extent1]
            WHERE [Extent1].[Id] = @p2
            """;

        var result = _interpreter.Interpret(sql);
        Assert.IsNotNull(result);
        Assert.AreEqual(EfSqlAccess.ReadWrite, result!.Access);

        var insert = result.Statements.Single(statement => statement.Kind == EfSqlStatementKind.Insert);
        Assert.AreEqual("Orders", insert.Insert!.Table.Name);
        CollectionAssert.AreEqual(new[] { "CustomerId", "OrderDate" }, insert.Insert.Columns.ToArray());
    }

    [TestMethod]
    public void Interpret_ClassifiesEf6Update_WithSetAndWhere()
    {
        const string sql = """
            UPDATE [dbo].[Orders]
            SET [Status] = @p0
            WHERE ([Extent1].[Id] = @p1)
            """;

        var result = _interpreter.Interpret(sql);
        Assert.IsNotNull(result);
        Assert.AreEqual(EfSqlAccess.ReadWrite, result!.Access);

        var update = result.Statements.Single(statement => statement.Kind == EfSqlStatementKind.Update);
        Assert.AreEqual("Orders", update.Update!.Table.Name);
        Assert.AreEqual("Status", update.Update.Assignments[0].Column);
        Assert.AreEqual("@p0", update.Update.Assignments[0].NewValueExpression);
        Assert.IsTrue(update.Update.WhereSql!.Contains("@p1", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Interpret_DetectsAndParsesEf6Update_WithRpcParameterPreamble()
    {
        const string sql = "(@0 int,@1 datetime)UPDATE [dbo].[Orders] SET [Status] = @0 WHERE [Id] = @1";

        var result = _interpreter.Interpret(sql);
        Assert.IsNotNull(result);
        Assert.AreEqual(EfSqlAccess.ReadWrite, result!.Access);

        var update = result.Statements.Single(statement => statement.Kind == EfSqlStatementKind.Update);
        Assert.AreEqual("Orders", update.Update!.Table.Name);
        Assert.AreEqual("Status", update.Update.Assignments[0].Column);
    }
}
