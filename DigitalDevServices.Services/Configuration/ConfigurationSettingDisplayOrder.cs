using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Services.Configuration;

public static class ConfigurationSettingDisplayOrder
{
    public static IReadOnlyList<ConfigurationSetting> SortSettings(
        IEnumerable<ConfigurationSetting> settings,
        IReadOnlyList<PinnedConfigurationKey> pinnedKeys)
    {
        var pinnedOrder = BuildPinnedOrderIndex(pinnedKeys);
        var settingsList = settings.ToList();

        var pinned = settingsList
            .Where(setting => pinnedOrder.ContainsKey(setting.Key))
            .OrderBy(setting => pinnedOrder[setting.Key])
            .ThenBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unpinned = settingsList
            .Where(setting => !pinnedOrder.ContainsKey(setting.Key))
            .OrderBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return pinned.Concat(unpinned).ToList();
    }

    public static IReadOnlyList<ConfigurationComparisonRow> SortComparisonRows(
        IEnumerable<ConfigurationComparisonRow> rows,
        IReadOnlyList<PinnedConfigurationKey> pinnedKeys)
    {
        var pinnedOrder = BuildPinnedOrderIndex(pinnedKeys);
        var rowsList = rows.ToList();

        var pinned = rowsList
            .Where(row => pinnedOrder.ContainsKey(row.Key))
            .OrderBy(row => pinnedOrder[row.Key])
            .ThenBy(row => row.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unpinned = rowsList
            .Where(row => !pinnedOrder.ContainsKey(row.Key))
            .OrderBy(row => row.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return pinned.Concat(unpinned).ToList();
    }

    private static Dictionary<string, int> BuildPinnedOrderIndex(
        IReadOnlyList<PinnedConfigurationKey> pinnedKeys)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var order = 0;

        foreach (var pinnedKey in pinnedKeys
                     .OrderBy(pinned => pinned.DisplayOrder)
                     .ThenBy(pinned => pinned.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!index.ContainsKey(pinnedKey.Key))
            {
                index[pinnedKey.Key] = order++;
            }
        }

        return index;
    }
}
