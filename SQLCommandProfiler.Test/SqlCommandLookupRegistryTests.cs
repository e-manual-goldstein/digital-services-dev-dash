namespace SQLCommandProfiler.Test;

[TestClass]
public sealed class SqlCommandLookupRegistryTests
{
    [TestMethod]
    public void Match_FindsCommand_ByFullOrShortName()
    {
        var path = Path.Combine(Path.GetTempPath(), $"SqlCommandLookup-{Guid.NewGuid():N}.json");
        File.WriteAllText(
            path,
            """
            {
              "commands": [
                { "name": "dbo.usp_GetData", "access": "ReadOnly" },
                { "name": "usp_Save", "access": "ReadWrite" }
              ]
            }
            """);

        try
        {
            var registry = SqlCommandLookupRegistry.Load(path);

            var full = registry.Match("dbo.usp_GetData");
            Assert.IsTrue(full.IsKnown);
            Assert.AreEqual(SqlCommandAccess.ReadOnly, full.Access);

            var shortName = registry.Match("usp_Save");
            Assert.IsTrue(shortName.IsKnown);
            Assert.AreEqual(SqlCommandAccess.ReadWrite, shortName.Access);

            var unknown = registry.Match("dbo.usp_Missing");
            Assert.IsFalse(unknown.IsKnown);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
