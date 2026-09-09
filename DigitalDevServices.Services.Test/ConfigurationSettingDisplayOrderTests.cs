using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Services.Configuration;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class ConfigurationSettingDisplayOrderTests
{
    [TestMethod]
    public void SortSettings_PlacesPinnedKeysFirstInDisplayOrder()
    {
        var pinnedKeys = new[]
        {
            CreatePinnedKey("FeatureFlags:NewCheckout", 1),
            CreatePinnedKey("ConnectionStrings:Default", 0)
        };
        var settings = new[]
        {
            CreateSetting("Logging:Level", "Information"),
            CreateSetting("FeatureFlags:NewCheckout", "true"),
            CreateSetting("ConnectionStrings:Default", "Server=localhost;"),
            CreateSetting("Api:BaseUrl", "https://example/api")
        };

        var sorted = ConfigurationSettingDisplayOrder.SortSettings(settings, pinnedKeys);

        Assert.AreEqual("ConnectionStrings:Default", sorted[0].Key);
        Assert.AreEqual("FeatureFlags:NewCheckout", sorted[1].Key);
        Assert.AreEqual("Api:BaseUrl", sorted[2].Key);
        Assert.AreEqual("Logging:Level", sorted[3].Key);
    }

    [TestMethod]
    public void SortComparisonRows_PlacesPinnedKeysFirstInDisplayOrder()
    {
        var pinnedKeys = new[] { CreatePinnedKey("Shared:Key", 0) };
        var rows = new[]
        {
            CreateRow("Other:Key"),
            CreateRow("Shared:Key")
        };

        var sorted = ConfigurationSettingDisplayOrder.SortComparisonRows(rows, pinnedKeys);

        Assert.AreEqual("Shared:Key", sorted[0].Key);
        Assert.AreEqual("Other:Key", sorted[1].Key);
    }

    private static PinnedConfigurationKey CreatePinnedKey(string key, int displayOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            DisplayOrder = displayOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ConfigurationSetting CreateSetting(string key, string value) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApplicationInstanceId = Guid.NewGuid(),
            Key = key,
            Value = value,
            CapturedAt = DateTimeOffset.UtcNow
        };

    private static ConfigurationComparisonRow CreateRow(string key) =>
        new()
        {
            Key = key,
            LeftValue = "left",
            RightValue = "right",
            Status = ConfigurationComparisonStatus.Match
        };
}
