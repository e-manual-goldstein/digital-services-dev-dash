using DigitalDevServices.Services.Logs;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class LogParserTests
{
    [TestMethod]
    public void SerilogJsonLogParser_ParsesOneEntryPerLine()
    {
        const string content = """
            {"@t":"2026-08-23T08:15:02.1123456+01:00","@l":"Information","@mt":"Application starting","Environment":"UAT-01"}
            {"@t":"2026-08-23T08:15:18.4400000+01:00","@l":"Error","@mt":"Payment failed","OrderId":"ORD-1"}
            """;

        var entries = new SerilogJsonLogParser().Parse(content);

        Assert.HasCount(2, entries);
        Assert.AreEqual("Information", entries[0].Level);
        Assert.AreEqual("Application starting", entries[0].Message);
        Assert.AreEqual("UAT-01", entries[0].Properties!["Environment"]);
        Assert.AreEqual("Error", entries[1].Level);
    }

    [TestMethod]
    public void PlainTextLogParser_ParsesSingleLineEntries()
    {
        const string content = """
            2026-08-23 09:02:11.004 INFO  [WorkerHost] Background worker started
            2026-08-23 09:03:01.774 WARN  [ImportJob] Row 88 skipped
            """;

        var entries = new PlainTextLogParser().Parse(content);

        Assert.HasCount(2, entries);
        Assert.AreEqual("INFO", entries[0].Level);
        Assert.AreEqual("WorkerHost", entries[0].Properties!["Logger"]);
        Assert.AreEqual("WARN", entries[1].Level);
    }

    [TestMethod]
    public void NLogMultilineLogParser_GroupsStackTracesWithHeader()
    {
        const string content = """
            2026-08-23 10:12:44.2200|ERROR|Billing.Api.Services.InvoiceRepository|Failed to load invoice 5541
            System.Data.SqlClient.SqlException: Timeout expired.
               at Billing.Api.Data.InvoiceRepository.GetByIdAsync(Int32 invoiceId)
            2026-08-23 10:12:45.0055|INFO|Billing.Api.Middleware.ExceptionMiddleware|Returning 503
            """;

        var entries = new NLogMultilineLogParser().Parse(content);

        Assert.HasCount(2, entries);
        Assert.AreEqual("ERROR", entries[0].Level);
        StringAssert.Contains(entries[0].Message, "Timeout expired");
        StringAssert.Contains(entries[0].Message, "GetByIdAsync");
        Assert.AreEqual("INFO", entries[1].Level);
    }

    [TestMethod]
    public void Log4NetPatternLogParser_GroupsExceptionBlocks()
    {
        const string content = """
            2026-08-23 11:22:10,002 [15] ERROR Portal.Web.Controllers.DocumentController - Unhandled exception
            System.IO.FileNotFoundException: Could not find file 'summary.pdf'.
               at Portal.Services.DocumentStorage.OpenRead(String relativePath)
            2026-08-23 11:22:10,115 [15] INFO  Portal.Web.MvcApplication - HTTP 404 returned
            """;

        var entries = new Log4NetPatternLogParser().Parse(content);

        Assert.HasCount(2, entries);
        Assert.AreEqual("ERROR", entries[0].Level);
        StringAssert.Contains(entries[0].Message, "FileNotFoundException");
        Assert.AreEqual("15", entries[0].Properties!["Thread"]);
    }

    [TestMethod]
    public void SampleLogFiles_ParseWithExpectedEntryCounts()
    {
        var samplesDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "logs"));
        if (!Directory.Exists(samplesDirectory))
        {
            Assert.Inconclusive("Sample logs directory was not found.");
        }

        Assert.IsGreaterThanOrEqualTo(8, new SerilogJsonLogParser().Parse(File.ReadAllText(Path.Combine(samplesDirectory, "serilog-json.log"))).Count);
        Assert.IsGreaterThanOrEqualTo(8, new PlainTextLogParser().Parse(File.ReadAllText(Path.Combine(samplesDirectory, "plain-text.log"))).Count);
        Assert.IsGreaterThanOrEqualTo(7, new NLogMultilineLogParser().Parse(File.ReadAllText(Path.Combine(samplesDirectory, "nlog-multiline.log"))).Count);
        Assert.IsGreaterThanOrEqualTo(7, new Log4NetPatternLogParser().Parse(File.ReadAllText(Path.Combine(samplesDirectory, "log4net-pattern.log"))).Count);
    }
}
