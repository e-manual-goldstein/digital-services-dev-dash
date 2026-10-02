namespace SQLCommandProfiler.Test;

[TestClass]
public sealed class ProfilerLiveEventClassifierTests
{
    [TestMethod]
    public void TryCreateDisplay_ClassifiesInsert_WithTableSubtitle()
    {
        const string sql = """
            INSERT [dbo].[Orders]([CustomerId])
            VALUES (@p0)
            """;

        var interpreter = new EfSqlInterpreter();
        var captured = new CapturedSqlEvent
        {
            Info = new ExtendedEventInfo(
                TimestampUtc: DateTimeOffset.UtcNow,
                EventName: "rpc_completed",
                ObjectName: string.Empty,
                DatabaseName: "Shop",
                UserName: "u",
                ApplicationName: "app",
                HostName: "h",
                SessionId: 1,
                ClientProcessId: 0,
                SqlText: sql,
                DurationMicroseconds: 1,
                CpuMicroseconds: 1,
                LogicalReads: 1,
                QueryHash: 1,
                QueryPlanHash: 0),
            EfInterpretation = interpreter.Interpret(sql),
        };

        Assert.IsTrue(ProfilerLiveEventClassifier.TryCreateDisplay(captured, out var display));
        Assert.AreEqual(ProfilerEventBucket.Insert, display.Bucket);
        Assert.AreEqual("ORDERS", display.Title);
        Assert.AreEqual("[Shop].[dbo].[Orders]", display.Subtitle);
    }

    [TestMethod]
    public void CreateUnknownDisplay_UsesObjectNameAsTitle()
    {
        var captured = new CapturedSqlEvent
        {
            Info = new ExtendedEventInfo(
                TimestampUtc: DateTimeOffset.UtcNow,
                EventName: "rpc_completed",
                ObjectName: "dbo.usp_Custom",
                DatabaseName: "Shop",
                UserName: "u",
                ApplicationName: "app",
                HostName: "h",
                SessionId: 1,
                ClientProcessId: 0,
                SqlText: "EXEC dbo.usp_Custom",
                DurationMicroseconds: 1,
                CpuMicroseconds: 1,
                LogicalReads: 1,
                QueryHash: 1,
                QueryPlanHash: 0),
        };

        var display = ProfilerLiveEventClassifier.CreateUnknownDisplay(captured);

        Assert.AreEqual(ProfilerEventBucket.Unknown, display.Bucket);
        Assert.AreEqual("usp_Custom", display.Title);
    }

    [TestMethod]
    public void CreateUnknownDisplay_UsesTruncatedSqlText_ForSpExecuteSql()
    {
        var longSql = "SELECT " + new string('x', 200);

        var captured = new CapturedSqlEvent
        {
            Info = new ExtendedEventInfo(
                TimestampUtc: DateTimeOffset.UtcNow,
                EventName: "rpc_completed",
                ObjectName: "sys.sp_executesql",
                DatabaseName: "Shop",
                UserName: "u",
                ApplicationName: "app",
                HostName: "h",
                SessionId: 1,
                ClientProcessId: 0,
                SqlText: longSql,
                DurationMicroseconds: 1,
                CpuMicroseconds: 1,
                LogicalReads: 1,
                QueryHash: 1,
                QueryPlanHash: 0),
        };

        var display = ProfilerLiveEventClassifier.CreateUnknownDisplay(captured);

        Assert.IsTrue(display.Title.StartsWith("SELECT ", StringComparison.Ordinal));
        Assert.IsTrue(display.Title.EndsWith("...", StringComparison.Ordinal));
        Assert.AreEqual("Shop", display.Subtitle);
    }

    [TestMethod]
    public void TryCreateDisplay_ClassifiesRecognisedCommand_FromLookup()
    {
        var captured = new CapturedSqlEvent
        {
            Info = new ExtendedEventInfo(
                TimestampUtc: DateTimeOffset.UtcNow,
                EventName: "rpc_completed",
                ObjectName: "dbo.usp_ExampleRead",
                DatabaseName: "Shop",
                UserName: "u",
                ApplicationName: "app",
                HostName: "h",
                SessionId: 1,
                ClientProcessId: 0,
                SqlText: "EXEC dbo.usp_ExampleRead",
                DurationMicroseconds: 1,
                CpuMicroseconds: 1,
                LogicalReads: 1,
                QueryHash: 1,
                QueryPlanHash: 0),
            CommandLookup = new SqlCommandLookupMatch
            {
                ObjectName = "dbo.usp_ExampleRead",
                Access = SqlCommandAccess.ReadOnly,
                IsKnown = true,
            },
        };

        Assert.IsTrue(ProfilerLiveEventClassifier.TryCreateDisplay(captured, out var display));
        Assert.AreEqual(ProfilerEventBucket.RecognisedCommand, display.Bucket);
        Assert.AreEqual("usp_ExampleRead", display.Title);
    }
}
