using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public static class CoverletCoverageSummaryCalculator
{
    public static CoverletCoverageSummary BuildFromRows(IReadOnlyList<CoverletCoverageRow> rows)
    {
        if (rows.Count == 0)
        {
            return Empty();
        }

        var coveredLines = rows.Sum(row => row.CoveredLines);
        var coverableLines = rows.Sum(row => row.CoverableLines);
        var totalLines = rows.Sum(row => row.TotalLines);
        var coveredBranches = rows.Sum(row => row.CoveredBranches);
        var totalBranches = rows.Sum(row => row.TotalBranches);
        var coveredMethods = rows.Count(row => row.CoveredLines > 0 || row.LineCoveragePercent > 0);
        var totalMethods = rows.Count;

        return new CoverletCoverageSummary
        {
            CoveredLines = coveredLines,
            CoverableLines = coverableLines,
            TotalLines = totalLines,
            LineCoveragePercent = Percent(coveredLines, coverableLines),
            CoveredBranches = coveredBranches,
            TotalBranches = totalBranches,
            BranchCoveragePercent = Percent(coveredBranches, totalBranches),
            CoveredMethods = coveredMethods,
            TotalMethods = totalMethods,
            MethodCoveragePercent = Percent(coveredMethods, totalMethods)
        };
    }

    private static CoverletCoverageSummary Empty() =>
        new()
        {
            CoveredLines = 0,
            CoverableLines = 0,
            TotalLines = 0,
            LineCoveragePercent = 0,
            CoveredBranches = 0,
            TotalBranches = 0,
            BranchCoveragePercent = 0,
            CoveredMethods = 0,
            TotalMethods = 0,
            MethodCoveragePercent = 0
        };

    private static decimal Percent(int covered, int total) =>
        total == 0 ? 0 : Math.Round(covered * 100m / total, 2);
}
