using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler.Test;

[TestClass]
public sealed class ProfilerConnectionResolverTests
{
    [TestMethod]
    public void Resolve_ReplacesDataSource_WhenInstanceProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=default-host;Database=Shop;Trusted_Connection=True;Encrypt=False",
            })
            .Build();

        var resolved = ProfilerConnectionResolver.Resolve(configuration, "other-host\\INSTANCE");

        StringAssert.Contains(resolved, "other-host\\INSTANCE");
        Assert.IsFalse(resolved.Contains("default-host", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Resolve_ReturnsDefaultConnectionString_WhenInstanceEmpty()
    {
        const string connectionString = "Server=default-host;Database=Shop;Trusted_Connection=True;Encrypt=False";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
            })
            .Build();

        var resolved = ProfilerConnectionResolver.Resolve(configuration, "  ");

        Assert.AreEqual(connectionString, resolved);
    }
}
