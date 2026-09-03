using DigitalDevServices.Model.TextFormatting;
using DigitalDevServices.Services.TextFormatting;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class FormattedTextServiceTests
{
    private readonly FormattedTextService _service = new();

    [TestMethod]
    public void Format_Json_PrettyPrintsSingleObject()
    {
        const string input = """{"name":"app","enabled":true}""";

        var result = _service.Format(input, FormattedTextDisplayFormat.Json);

        Assert.IsTrue(result.IsFormatted);
        Assert.IsNull(result.Hint);
        StringAssert.Contains(result.DisplayText, "\"name\": \"app\"");
        StringAssert.Contains(result.DisplayText, "\"enabled\": true");
    }

    [TestMethod]
    public void Format_Json_PrettyPrintsNdJsonLines()
    {
        const string input = """
            {"level":"Information","message":"Started"}
            {"level":"Warning","message":"Retry"}
            not-json
            """;

        var result = _service.Format(input, FormattedTextDisplayFormat.Json);

        Assert.IsTrue(result.IsFormatted);
        StringAssert.Contains(result.DisplayText, "\"level\": \"Information\"");
        StringAssert.Contains(result.DisplayText, "\"level\": \"Warning\"");
        StringAssert.Contains(result.DisplayText, "not-json");
        Assert.IsNotNull(result.Hint);
        StringAssert.Contains(result.Hint!, "not valid JSON");
    }

    [TestMethod]
    public void Format_Json_ShowsHintForInvalidInput()
    {
        const string input = "plain text";

        var result = _service.Format(input, FormattedTextDisplayFormat.Json);

        Assert.IsFalse(result.IsFormatted);
        Assert.AreEqual(input, result.DisplayText);
        Assert.AreEqual("Not valid JSON.", result.Hint);
    }

    [TestMethod]
    public void Format_Xml_PrettyPrintsDocument()
    {
        const string input = "<root><item>value</item></root>";

        var result = _service.Format(input, FormattedTextDisplayFormat.Xml);

        Assert.IsTrue(result.IsFormatted);
        Assert.IsNull(result.Hint);
        StringAssert.Contains(result.DisplayText, "<root>");
        StringAssert.Contains(result.DisplayText, "  <item>value</item>");
    }

    [TestMethod]
    public void Format_Xml_ShowsHintForInvalidInput()
    {
        const string input = "<root><unclosed>";

        var result = _service.Format(input, FormattedTextDisplayFormat.Xml);

        Assert.IsFalse(result.IsFormatted);
        Assert.AreEqual(input, result.DisplayText);
        Assert.AreEqual("Not valid XML.", result.Hint);
    }

    [TestMethod]
    public void Format_Raw_ReturnsSourceText()
    {
        const string input = """{"name":"app"}""";

        var result = _service.Format(input, FormattedTextDisplayFormat.Raw);

        Assert.IsFalse(result.IsFormatted);
        Assert.AreEqual(input, result.DisplayText);
        Assert.IsNull(result.Hint);
    }

    [TestMethod]
    public void DetectAutoFormat_SelectsJsonForObject()
    {
        const string input = """{"name":"app"}""";

        var format = _service.DetectAutoFormat(input);

        Assert.AreEqual(FormattedTextDisplayFormat.Json, format);
    }

    [TestMethod]
    public void DetectAutoFormat_SelectsJsonForNdJsonTail()
    {
        const string input = """
            {"a":1}
            {"b":2}
            """;

        var format = _service.DetectAutoFormat(input);

        Assert.AreEqual(FormattedTextDisplayFormat.Json, format);
    }

    [TestMethod]
    public void DetectAutoFormat_SelectsXmlForDocument()
    {
        const string input = "<root><item /></root>";

        var format = _service.DetectAutoFormat(input);

        Assert.AreEqual(FormattedTextDisplayFormat.Xml, format);
    }

    [TestMethod]
    public void DetectAutoFormat_ReturnsNullForPlainText()
    {
        var format = _service.DetectAutoFormat("hello world");

        Assert.IsNull(format);
    }
}
