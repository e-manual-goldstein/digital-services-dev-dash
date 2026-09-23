using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public static class CoverletCoverageRowFilter
{
    public static IReadOnlyList<CoverletCoverageRow> Apply(
        IEnumerable<CoverletCoverageRow> rows,
        CoverletCoverageFilterState filters)
    {
        return rows.Where(row => Matches(row, filters)).ToList();
    }

    public static bool Matches(CoverletCoverageRow row, CoverletCoverageFilterState filters)
    {
        if (!ContainsIgnoreCase(row.Module, filters.ModuleContains))
        {
            return false;
        }

        if (!ContainsIgnoreCase(row.SourceFile, filters.SourceFileContains))
        {
            return false;
        }

        if (!ContainsIgnoreCase(row.ClassName, filters.ClassContains))
        {
            return false;
        }

        if (!ContainsIgnoreCase(row.MethodName, filters.MethodContains))
        {
            return false;
        }

        if (filters.MinLineCoveragePercent is not null
            && row.LineCoveragePercent < filters.MinLineCoveragePercent.Value)
        {
            return false;
        }

        if (filters.MinBranchCoveragePercent is not null
            && row.BranchCoveragePercent < filters.MinBranchCoveragePercent.Value)
        {
            return false;
        }

        if (filters.MinCoveredLines is not null
            && row.CoveredLines < filters.MinCoveredLines.Value)
        {
            return false;
        }

        if (filters.MinCoveredBranches is not null
            && row.CoveredBranches < filters.MinCoveredBranches.Value)
        {
            return false;
        }

        return true;
    }

    private static bool ContainsIgnoreCase(string value, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return value.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
