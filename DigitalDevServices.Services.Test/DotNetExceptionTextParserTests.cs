using DigitalDevServices.Services.Logs;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class DotNetExceptionTextParserTests
{
    [TestMethod]
    public void TryParse_ParsesSingleExceptionWithStackTrace()
    {
        const string text = """
            System.Net.Http.HttpRequestException: Connection timed out
               at PaymentClient.PostAsync(String url)
               at CheckoutService.ChargeAsync(Guid orderId)
            """;

        var exception = DotNetExceptionTextParser.TryParse(text);

        Assert.IsNotNull(exception);
        Assert.AreEqual("System.Net.Http.HttpRequestException", exception!.Type);
        Assert.AreEqual("Connection timed out", exception.Message);
        StringAssert.Contains(exception.StackTrace!, "PaymentClient.PostAsync");
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public void TryParse_UnwindsInnerExceptionChain()
    {
        const string text = """
            System.Data.SqlClient.SqlException (0x80131904): Timeout expired.
             ---> System.ComponentModel.Win32Exception (258): The wait operation timed out.
               at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
               at Billing.Api.Data.InvoiceRepository.GetByIdAsync(Int32 invoiceId)
            """;

        var exception = DotNetExceptionTextParser.TryParse(text);

        Assert.IsNotNull(exception);
        Assert.AreEqual("System.Data.SqlClient.SqlException (0x80131904)", exception!.Type);
        Assert.AreEqual("Timeout expired.", exception.Message);
        Assert.IsNotNull(exception.InnerException);
        Assert.AreEqual("System.ComponentModel.Win32Exception (258)", exception.InnerException!.Type);
        Assert.AreEqual("The wait operation timed out.", exception.InnerException.Message);
    }

    [TestMethod]
    public void SerilogJsonLogParser_PopulatesStructuredExceptionWithoutAppendingToMessage()
    {
        const string content = """
            {"@timestamp":"2026-08-23T08:15:18.4400000+01:00","log.level":"Error","message":"Payment failed","error":{"message":"Connection timed out","stack_trace":"System.Net.Http.HttpRequestException: Connection timed out\n   at PaymentClient.PostAsync(String url)"}}
            """;

        var entries = new SerilogJsonLogParser().Parse(content);

        Assert.HasCount(1, entries);
        Assert.AreEqual("Payment failed", entries[0].Message);
        Assert.IsNotNull(entries[0].Exception);
        Assert.AreEqual("System.Net.Http.HttpRequestException", entries[0].Exception!.Type);
        Assert.AreEqual("Connection timed out", entries[0].Exception.Message);
    }

    [TestMethod]
    public void NLogMultilineLogParser_SplitsSummaryMessageAndException()
    {
        const string content = """
            2026-08-23 10:12:44.2200|ERROR|Billing.Api.Services.InvoiceRepository|Failed to load invoice 5541
            System.Data.SqlClient.SqlException (0x80131904): Timeout expired.
             ---> System.ComponentModel.Win32Exception (258): The wait operation timed out.
               at Billing.Api.Data.InvoiceRepository.GetByIdAsync(Int32 invoiceId)
            """;

        var entries = new NLogMultilineLogParser().Parse(content);

        Assert.HasCount(1, entries);
        Assert.AreEqual("Failed to load invoice 5541", entries[0].Message);
        Assert.IsNotNull(entries[0].Exception);
        Assert.AreEqual("System.Data.SqlClient.SqlException (0x80131904)", entries[0].Exception!.Type);
        Assert.IsNotNull(entries[0].Exception.InnerException);
    }
}
