using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Services.Configuration;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class ConfigurationSettingComparerTests
{
    [TestMethod]
    public void Compare_HighlightsMismatchesAndMissingKeys()
    {
        var leftSettings = new[]
        {
            CreateSetting("FeatureFlags:Beta", "true"),
            CreateSetting("Shared:Key", "left"),
            CreateSetting("LeftOnly:Key", "value")
        };
        var rightSettings = new[]
        {
            CreateSetting("FeatureFlags:Beta", "false"),
            CreateSetting("Shared:Key", "left"),
            CreateSetting("RightOnly:Key", "value")
        };

        var rows = ConfigurationSettingComparer.Compare(leftSettings, rightSettings);
        var rowsByKey = rows.ToDictionary(row => row.Key);

        Assert.AreEqual(ConfigurationComparisonStatus.Mismatch, rowsByKey["FeatureFlags:Beta"].Status);
        Assert.AreEqual(ConfigurationComparisonStatus.Match, rowsByKey["Shared:Key"].Status);
        Assert.AreEqual(ConfigurationComparisonStatus.LeftOnly, rowsByKey["LeftOnly:Key"].Status);
        Assert.AreEqual(ConfigurationComparisonStatus.RightOnly, rowsByKey["RightOnly:Key"].Status);
    }

    private static ConfigurationSetting CreateSetting(string key, string value) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApplicationInstanceId = Guid.NewGuid(),
            Key = key,
            Value = value,
            CapturedAt = DateTimeOffset.UtcNow
        };
}
