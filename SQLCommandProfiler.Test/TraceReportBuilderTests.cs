namespace SQLCommandProfiler.Test;

[TestClass]
public sealed class TraceReportBuilderTests
{
    [TestMethod]
    public void Build_GroupsByApplication_AndSummarisesEfEntities()
    {
        var insertSql = """
            INSERT [dbo].[Orders]([CustomerId])
            VALUES (@p0)
            """;

        var interpreter = new EfSqlInterpreter();
        var events = new List<CapturedSqlEvent>
        {
            new()
            {
                Info = new ExtendedEventInfo(
                    TimestampUtc: DateTimeOffset.Parse("2026-01-01T12:00:00Z"),
                    EventName: "rpc_completed",
                    DatabaseName: "Shop",
                    UserName: "app",
                    ApplicationName: "WebApp",
                    HostName: "host1",
                    SessionId: 10,
                    SqlText: insertSql,
                    DurationMicroseconds: 1000,
                    CpuMicroseconds: 900,
                    LogicalReads: 2,
                    QueryHash: 0,
                    QueryPlanHash: 0),
                EfInterpretation = interpreter.Interpret(insertSql),
            },
            new()
            {
                Info = new ExtendedEventInfo(
                    TimestampUtc: DateTimeOffset.Parse("2026-01-01T12:00:01Z"),
                    EventName: "sql_statement_completed",
                    DatabaseName: "Shop",
                    UserName: "app",
                    ApplicationName: "Worker",
                    HostName: "host2",
                    SessionId: 11,
                    SqlText: "SELECT 1",
                    DurationMicroseconds: 500,
                    CpuMicroseconds: 400,
                    LogicalReads: 1,
                    QueryHash: 0,
                    QueryPlanHash: 0),
            },
        };

        var report = TraceReportBuilder.Build("TestSession", batchCount: 1, events, EfSqlInterpretationWhen.OnReceive);

        Assert.AreEqual(2, report.Summary.TotalEventsCaptured);
        Assert.AreEqual(1, report.EfCommandReport.EntitiesCreated);
        Assert.AreEqual("dbo.Orders", report.EfCommandReport.CreatesByTable.Single().Key);

        var webApp = report.CommandsByApplicationName.Single(group => group.ApplicationName == "WebApp");
        Assert.AreEqual(1, webApp.EventCount);
        Assert.AreEqual(1, webApp.EventsByType["rpc_completed"]);
    }
}
