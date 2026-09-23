namespace DigitalDevServices.Services.Coverlet;

public class CoverletCoverageFilterState
{
    public string ModuleContains { get; init; } = string.Empty;

    public string SourceFileContains { get; init; } = string.Empty;

    public string ClassContains { get; init; } = string.Empty;

    public string MethodContains { get; init; } = string.Empty;

    public decimal? MinLineCoveragePercent { get; init; }

    public decimal? MinBranchCoveragePercent { get; init; }

    public int? MinCoveredLines { get; init; }

    public int? MinCoveredBranches { get; init; }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(ModuleContains)
        || !string.IsNullOrWhiteSpace(SourceFileContains)
        || !string.IsNullOrWhiteSpace(ClassContains)
        || !string.IsNullOrWhiteSpace(MethodContains)
        || MinLineCoveragePercent is not null
        || MinBranchCoveragePercent is not null
        || MinCoveredLines is not null
        || MinCoveredBranches is not null;
}
