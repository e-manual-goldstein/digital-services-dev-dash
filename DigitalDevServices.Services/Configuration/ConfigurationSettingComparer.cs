using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Services.Configuration;

public static class ConfigurationSettingComparer
{
    public static IReadOnlyList<ConfigurationComparisonRow> Compare(
        IReadOnlyList<ConfigurationSetting> leftSettings,
        IReadOnlyList<ConfigurationSetting> rightSettings)
    {
        var leftByKey = IndexSettings(leftSettings);
        var rightByKey = IndexSettings(rightSettings);
        var keys = leftByKey.Keys
            .Union(rightByKey.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<ConfigurationComparisonRow>(keys.Count);

        foreach (var key in keys)
        {
            leftByKey.TryGetValue(key, out var leftSetting);
            rightByKey.TryGetValue(key, out var rightSetting);

            rows.Add(new ConfigurationComparisonRow
            {
                Key = key,
                LeftValue = leftSetting?.Value,
                RightValue = rightSetting?.Value,
                LeftSource = leftSetting?.Source,
                RightSource = rightSetting?.Source,
                Status = ResolveStatus(leftSetting, rightSetting)
            });
        }

        return rows;
    }

    private static Dictionary<string, ConfigurationSetting> IndexSettings(
        IReadOnlyList<ConfigurationSetting> settings)
    {
        var index = new Dictionary<string, ConfigurationSetting>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in settings)
        {
            index[setting.Key] = setting;
        }

        return index;
    }

    private static ConfigurationComparisonStatus ResolveStatus(
        ConfigurationSetting? leftSetting,
        ConfigurationSetting? rightSetting)
    {
        if (leftSetting is null)
        {
            return ConfigurationComparisonStatus.RightOnly;
        }

        if (rightSetting is null)
        {
            return ConfigurationComparisonStatus.LeftOnly;
        }

        if (string.Equals(leftSetting.Value, rightSetting.Value, StringComparison.Ordinal))
        {
            return ConfigurationComparisonStatus.Match;
        }

        return ConfigurationComparisonStatus.Mismatch;
    }
}
