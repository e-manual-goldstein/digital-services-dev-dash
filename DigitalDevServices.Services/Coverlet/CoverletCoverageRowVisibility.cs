using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public static class CoverletCoverageRowVisibility
{
    public static IReadOnlyList<CoverletCoverageRow> Apply(
        IEnumerable<CoverletCoverageRow> rows,
        IReadOnlySet<string> hiddenRowKeys,
        bool showHidden)
    {
        return rows
            .Where(row => showHidden || !hiddenRowKeys.Contains(row.RowKey))
            .ToList();
    }
}
