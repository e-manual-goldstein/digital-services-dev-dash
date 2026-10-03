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
                    ObjectName: "dbo.usp_ExampleWrite",
                    DatabaseName: "Shop",
                    UserName: "app",
                    ApplicationName: "WebApp",
                    HostName: "host1",
                    SessionId: 10,
                    ClientProcessId: 4242,
                    SqlText: insertSql,
                    Statement: string.Empty,
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
                    ObjectName: string.Empty,
                    DatabaseName: "Shop",
                    UserName: "app",
                    ApplicationName: "Worker",
                    HostName: "host2",
                    SessionId: 11,
                    ClientProcessId: 0,
                    SqlText: "SELECT 1",
                    Statement: string.Empty,
                    DurationMicroseconds: 500,
                    CpuMicroseconds: 400,
                    LogicalReads: 1,
                    QueryHash: 0,
                    QueryPlanHash: 0),
            },
        };

        var report = TraceReportBuilder.Build("TestSession", batchCount: 1, events, EfSqlInterpretationWhen.OnReceive, sqlCommandLookupConfigured: false);

        Assert.AreEqual(2, report.Summary.TotalEventsCaptured);
        Assert.AreEqual(1, report.EfCommandReport.EntitiesCreated);
        Assert.AreEqual("dbo.Orders", report.EfCommandReport.CreatesByTable.Single().Key);

        var webApp = report.CommandsByApplicationName.Single(group => group.ApplicationName == "WebApp");
        Assert.AreEqual(1, webApp.EventCount);
        Assert.AreEqual(1, webApp.EventsByType["rpc_completed"]);
        Assert.AreEqual(4242, webApp.Events.Single().ClientProcessId);
    }

    [TestMethod]
    public void Build_GroupsDuplicateCommands_ByQueryHash()
    {
        const ulong sharedHash = 0x123456789ABCDEF0UL;
        var events = new List<CapturedSqlEvent>
        {
            CreateEventWithHash(sharedHash, "WebApp", "SELECT 1"),
            CreateEventWithHash(sharedHash, "WebApp", "SELECT 1"),
            CreateEventWithHash(sharedHash, "Worker", "SELECT 1"),
            CreateEventWithHash(0xFEDCBA0987654321UL, "WebApp", "SELECT 2"),
        };

        var report = TraceReportBuilder.Build("TestSession", 1, events, EfSqlInterpretationWhen.Never, sqlCommandLookupConfigured: false);

        Assert.AreEqual(4, report.DuplicateCommands.EventsWithQueryHash);
        Assert.AreEqual(2, report.DuplicateCommands.UniqueQueryHashes);
        Assert.AreEqual(1, report.DuplicateCommands.DuplicateQueryHashGroups);
        Assert.AreEqual(2, report.DuplicateCommands.RedundantInvocations);

        var group = report.DuplicateCommands.Groups.Single();
        Assert.AreEqual(sharedHash.ToString("X16"), group.QueryHash);
        Assert.AreEqual(3, group.InvocationCount);
    }

    [TestMethod]
    public void Build_OmitsBuiltInRpcs_FromReportSections_ButKeepsEfInterpretationForExecuteSql()
    {
        const string insertSql = """
            INSERT [dbo].[Orders]([CustomerId])
            VALUES (@p0)
            """;

        var interpreter = new EfSqlInterpreter();
        var events = new List<CapturedSqlEvent>
        {
            new()
            {
                Info = new ExtendedEventInfo(
                    TimestampUtc: DateTimeOffset.UtcNow,
                    EventName: "rpc_completed",
                    ObjectName: "sp_reset_connection",
                    DatabaseName: "Shop",
                    UserName: "app",
                    ApplicationName: "WebApp",
                    HostName: "host",
                    SessionId: 1,
                    ClientProcessId: 0,
                    SqlText: string.Empty,
                    Statement: string.Empty,
                    DurationMicroseconds: 100,
                    CpuMicroseconds: 100,
                    LogicalReads: 0,
                    QueryHash: 0,
                    QueryPlanHash: 0),
            },
            new()
            {
                Info = new ExtendedEventInfo(
                    TimestampUtc: DateTimeOffset.UtcNow,
                    EventName: "rpc_completed",
                    ObjectName: "sys.sp_executesql",
                    DatabaseName: "Shop",
                    UserName: "app",
                    ApplicationName: "WebApp",
                    HostName: "host",
                    SessionId: 1,
                    ClientProcessId: 0,
                    SqlText: insertSql,
                    Statement: string.Empty,
                    DurationMicroseconds: 1000,
                    CpuMicroseconds: 900,
                    LogicalReads: 2,
                    QueryHash: 0xABCDEFUL,
                    QueryPlanHash: 0),
                EfInterpretation = interpreter.Interpret(insertSql),
            },
            new()
            {
                Info = new ExtendedEventInfo(
                    TimestampUtc: DateTimeOffset.UtcNow,
                    EventName: "sql_statement_completed",
                    ObjectName: string.Empty,
                    DatabaseName: "Shop",
                    UserName: "app",
                    ApplicationName: "WebApp",
                    HostName: "host",
                    SessionId: 2,
                    ClientProcessId: 0,
                    SqlText: "SELECT 1",
                    Statement: string.Empty,
                    DurationMicroseconds: 500,
                    CpuMicroseconds: 400,
                    LogicalReads: 1,
                    QueryHash: 0,
                    QueryPlanHash: 0),
            },
        };

        var report = TraceReportBuilder.Build("TestSession", batchCount: 1, events, EfSqlInterpretationWhen.OnReceive, sqlCommandLookupConfigured: false);

        Assert.AreEqual(1, report.Summary.TotalEventsCaptured);
        Assert.AreEqual(1, report.EfCommandReport.EntitiesCreated);
        Assert.AreEqual(0, report.KnownSqlCommandReport.UnknownCommands);
        Assert.IsFalse(report.KnownSqlCommandReport.InvocationsByCommand.ContainsKey("sp_executesql"));
        Assert.AreEqual(1, report.CommandsByApplicationName.Single().EventCount);
    }

    [TestMethod]
    public void IsExcludedFromCapture_SkipsResetConnectionOnly()
    {
        Assert.IsTrue(BuiltInSqlServerRpc.IsExcludedFromCapture("sp_reset_connection"));
        Assert.IsFalse(BuiltInSqlServerRpc.IsExcludedFromCapture("sp_executesql"));
    }

    private static CapturedSqlEvent CreateEventWithHash(ulong queryHash, string applicationName, string sql)
    {
        return new CapturedSqlEvent
        {
            Info = new ExtendedEventInfo(
                TimestampUtc: DateTimeOffset.UtcNow,
                EventName: "rpc_completed",
                ObjectName: string.Empty,
                DatabaseName: "Shop",
                UserName: "app",
                ApplicationName: applicationName,
                HostName: "host",
                SessionId: 1,
                ClientProcessId: 0,
                SqlText: sql,
                Statement: string.Empty,
                DurationMicroseconds: 100,
                CpuMicroseconds: 100,
                LogicalReads: 1,
                QueryHash: queryHash,
                QueryPlanHash: 0),
        };
    }
}
