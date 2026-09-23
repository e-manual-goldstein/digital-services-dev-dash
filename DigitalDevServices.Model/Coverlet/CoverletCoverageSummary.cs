namespace DigitalDevServices.Model.Coverlet;

public class CoverletCoverageSummary
{
    public int CoveredLines { get; init; }

    public int CoverableLines { get; init; }

    public int TotalLines { get; init; }

    public decimal LineCoveragePercent { get; init; }

    public int CoveredBranches { get; init; }

    public int TotalBranches { get; init; }

    public decimal BranchCoveragePercent { get; init; }

    public int CoveredMethods { get; init; }

    public int TotalMethods { get; init; }

    public decimal MethodCoveragePercent { get; init; }
}
