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

    private static ExtendedEventInfo CreateEvent(string databaseName)
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
            SqlText: string.Empty,
            DurationMicroseconds: 0,
            CpuMicroseconds: 0,
            LogicalReads: 0,
            QueryHash: 0,
            QueryPlanHash: 0);
    }
}
