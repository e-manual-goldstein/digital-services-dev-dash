using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public static class CoverletCoverageSummaryBuilder
{
    public static IReadOnlyList<CoverletModuleSummaryRow> BuildModuleSummaries(
        IEnumerable<CoverletCoverageRow> rows,
        IReadOnlySet<string> hiddenRowKeys)
    {
        var visible = CoverletCoverageRowVisibility.Apply(rows, hiddenRowKeys, showHidden: false);

        return visible
            .GroupBy(row => row.Module, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var methodRows = group.ToList();
                var coveredLines = methodRows.Sum(row => row.CoveredLines);
                var coverableLines = methodRows.Sum(row => row.CoverableLines);

                return new CoverletModuleSummaryRow
                {
                    ModuleName = group.Key,
                    ClassCount = methodRows
                        .Select(row => row.ClassName)
                        .Distinct(StringComparer.Ordinal)
                        .Count(),
                    MethodCount = methodRows.Count,
                    LineCoveragePercent = Percent(coveredLines, coverableLines)
                };
            })
            .OrderBy(row => row.ModuleName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<CoverletClassSummaryRow> BuildClassSummaries(
        IEnumerable<CoverletCoverageRow> rows,
        IReadOnlySet<string> hiddenRowKeys,
        string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return [];
        }

        var visible = CoverletCoverageRowVisibility.Apply(rows, hiddenRowKeys, showHidden: false)
            .Where(row => string.Equals(row.Module, moduleName, StringComparison.OrdinalIgnoreCase));

        return visible
            .GroupBy(row => row.ClassName, StringComparer.Ordinal)
            .Select(group =>
            {
                var methodRows = group.ToList();
                var coveredLines = methodRows.Sum(row => row.CoveredLines);
                var coverableLines = methodRows.Sum(row => row.CoverableLines);

                return new CoverletClassSummaryRow
                {
                    ClassName = group.Key,
                    MethodCount = methodRows.Count,
                    LineCount = coverableLines,
                    LineCoveragePercent = Percent(coveredLines, coverableLines)
                };
            })
            .OrderBy(row => row.ClassName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static decimal Percent(int covered, int total) =>
        total == 0 ? 0 : Math.Round(covered * 100m / total, 2);
}
