using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public static class CoverletCoverageModuleHide
{
    public static IReadOnlySet<string> HideMethodsInModules(
        IEnumerable<CoverletCoverageRow> rows,
        IReadOnlySet<string> hiddenRowKeys,
        IReadOnlySet<string> moduleNames)
    {
        if (moduleNames.Count == 0)
        {
            return hiddenRowKeys;
        }

        var updated = new HashSet<string>(hiddenRowKeys, StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (moduleNames.Contains(row.Module))
            {
                updated.Add(row.RowKey);
            }
        }

        return updated;
    }
}
