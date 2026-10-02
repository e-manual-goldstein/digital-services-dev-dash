using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler.Test;

[TestClass]
public sealed class EventFilterEngineTests
{
    [TestMethod]
    public void PassesFilters_IgnoresRules_WhenIsActiveFalse()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Profiler:EventFilters:Rules:0:IsActive"] = "false",
                ["Profiler:EventFilters:Rules:0:Effect"] = "Exclude",
                ["Profiler:EventFilters:Rules:0:Field"] = "DatabaseName",
                ["Profiler:EventFilters:Rules:0:Pattern"] = ".*",
            })
            .Build();

        var engine = new EventFilterEngine(configuration);
        var eventInfo = CreateEvent(databaseName: "master");

        Assert.IsTrue(engine.PassesFilters(eventInfo));
    }

    [TestMethod]
    public void PassesFilters_AppliesRules_WhenIsActiveTrue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Profiler:EventFilters:Rules:0:IsActive"] = "true",
                ["Profiler:EventFilters:Rules:0:Effect"] = "Exclude",
                ["Profiler:EventFilters:Rules:0:Field"] = "DatabaseName",
                ["Profiler:EventFilters:Rules:0:Pattern"] = "^master$",
            })
            .Build();

        var engine = new EventFilterEngine(configuration);
        var eventInfo = CreateEvent(databaseName: "master");

        Assert.IsFalse(engine.PassesFilters(eventInfo));
    }

    [TestMethod]
    public void PassesFilters_ExcludesBySqlTextPattern()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Profiler:EventFilters:Rules:0:Effect"] = "Exclude",
                ["Profiler:EventFilters:Rules:0:Field"] = "SqlText",
                ["Profiler:EventFilters:Rules:0:Pattern"] = "sp_getapplock",
            })
            .Build();

        var engine = new EventFilterEngine(configuration);
        var blocked = CreateEvent(databaseName: "Shop", sqlText: "exec sp_getapplock @p0, @p1");
        var allowed = CreateEvent(databaseName: "Shop", sqlText: "SELECT 1");

        Assert.IsFalse(engine.PassesFilters(blocked));
        Assert.IsTrue(engine.PassesFilters(allowed));
    }

    [TestMethod]
    public void TryAddRule_AppliesAfterConfigurationRules()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var engine = new EventFilterEngine(configuration);
        Assert.IsTrue(engine.TryAddRule(
            new ProfilerEventFilterDefinition(
                ProfilerEventFilterEffect.Exclude,
                ProfilerEventFilterField.DatabaseName,
                "^Shop$"),
            out _));

        var blocked = CreateEvent(databaseName: "Shop");
        var allowed = CreateEvent(databaseName: "Other");

        Assert.IsFalse(engine.PassesFilters(blocked));
        Assert.IsTrue(engine.PassesFilters(allowed));
    }

    [TestMethod]
    public void TryRemoveRuntimeRule_RestoresPassingEvents()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var engine = new EventFilterEngine(configuration);
        Assert.IsTrue(engine.TryAddRule(
            new ProfilerEventFilterDefinition(
                ProfilerEventFilterEffect.Exclude,
                ProfilerEventFilterField.DatabaseName,
                "^Shop$"),
            out _));

        var shopEvent = CreateEvent(databaseName: "Shop");
        Assert.IsFalse(engine.PassesFilters(shopEvent));

        Assert.IsTrue(engine.TryRemoveRuntimeRule(0, out _));
        Assert.IsTrue(engine.PassesFilters(shopEvent));
    }

    private static ExtendedEventInfo CreateEvent(string databaseName, string sqlText = "")
    {
        return new ExtendedEventInfo(
            TimestampUtc: DateTimeOffset.UtcNow,
            EventName: "rpc_completed",
            ObjectName: string.Empty,
            DatabaseName: databaseName,
            UserName: string.Empty,
            ApplicationName: string.Empty,
            HostName: string.Empty,
            SessionId: 1,
            ClientProcessId: 0,
            SqlText: sqlText,
            DurationMicroseconds: 0,
            CpuMicroseconds: 0,
            LogicalReads: 0,
            QueryHash: 0,
            QueryPlanHash: 0);
    }
}
