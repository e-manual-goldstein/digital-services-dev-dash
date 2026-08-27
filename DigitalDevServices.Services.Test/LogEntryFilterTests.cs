using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Logs;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class LogEntryFilterTests
{
    [TestMethod]
    public void Apply_FiltersByMinimumLevel()
    {
        var entries = new[]
        {
            Entry("INFO", "started"),
            Entry("WARN", "slow"),
            Entry("ERROR", "failed")
        };

        var filtered = LogEntryFilter.Apply(entries, LogEntryFilter.MinimumLevelWarning, messageContains: null);

        Assert.HasCount(2, filtered);
        Assert.AreEqual("WARN", filtered[0].Level);
        Assert.AreEqual("ERROR", filtered[1].Level);
    }

    [TestMethod]
    public void Apply_FiltersByMessageSearch()
    {
        var entries = new[]
        {
            Entry("INFO", "Request completed"),
            Entry("ERROR", "Connection timeout"),
            Entry("ERROR", "Payment timeout")
        };

        var filtered = LogEntryFilter.Apply(entries, minimumLevel: null, messageContains: "timeout");

        Assert.HasCount(2, filtered);
        Assert.IsTrue(filtered.All(entry => entry.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Apply_AppliesLevelAndSearchTogether()
    {
        var entries = new[]
        {
            Entry("INFO", "timeout waiting"),
            Entry("WARN", "timeout waiting"),
            Entry("ERROR", "timeout waiting")
        };

        var filtered = LogEntryFilter.Apply(entries, LogEntryFilter.MinimumLevelWarning, "timeout");

        Assert.HasCount(2, filtered);
        Assert.AreEqual("WARN", filtered[0].Level);
        Assert.AreEqual("ERROR", filtered[1].Level);
    }

    [TestMethod]
    public void MatchesMinimumLevel_NormalizesInformationAlias()
    {
        Assert.IsTrue(LogEntryFilter.MatchesMinimumLevel("INFORMATION", LogEntryFilter.MinimumLevelInformation));
        Assert.IsFalse(LogEntryFilter.MatchesMinimumLevel("DEBUG", LogEntryFilter.MinimumLevelInformation));
    }

    private static ParsedLogEntry Entry(string level, string message) =>
        new()
        {
            Level = level,
            Message = message,
            RawText = message
        };
}
