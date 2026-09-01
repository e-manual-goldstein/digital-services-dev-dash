using DigitalDevServices.Services.Logs;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class LogFileTailReaderTests
{
    [TestMethod]
    public async Task ReadAppendAsync_ReturnsEmptyWhenAtEndOfFile()
    {
        var filePath = CreateTempLogFile("line one\nline two\n");

        var result = await LogFileTailReader.ReadAppendAsync(filePath, new FileInfo(filePath).Length);

        Assert.IsFalse(result.HasNewContent);
        Assert.IsFalse(result.WasTruncated);
        Assert.AreEqual(new FileInfo(filePath).Length, result.EndPosition);
    }

    [TestMethod]
    public async Task ReadAppendAsync_ReturnsNewContentFromPosition()
    {
        var filePath = CreateTempLogFile("line one\n");
        var startPosition = new FileInfo(filePath).Length;

        await File.AppendAllTextAsync(filePath, "line two\nline three\n");

        var result = await LogFileTailReader.ReadAppendAsync(filePath, startPosition);

        Assert.IsTrue(result.HasNewContent);
        Assert.IsFalse(result.WasTruncated);
        Assert.AreEqual("line two\nline three\n", result.Content);
        Assert.AreEqual(new FileInfo(filePath).Length, result.EndPosition);
    }

    [TestMethod]
    public async Task ReadAppendAsync_DetectsTruncationWhenFileShrinks()
    {
        var filePath = CreateTempLogFile("line one\nline two\nline three\n");

        await File.WriteAllTextAsync(filePath, "line one\n");

        var result = await LogFileTailReader.ReadAppendAsync(filePath, 100);

        Assert.IsTrue(result.WasTruncated);
        Assert.IsFalse(result.HasNewContent);
        Assert.AreEqual(0, result.EndPosition);
    }

    private static string CreateTempLogFile(string content)
    {
        var filePath = Path.Combine(Path.GetTempPath(), "devdash-tail-" + Guid.NewGuid().ToString("N") + ".log");

        try
        {
            File.WriteAllText(filePath, content);
            return filePath;
        }
        catch
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            throw;
        }
    }
}
