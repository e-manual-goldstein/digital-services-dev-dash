namespace DigitalDevServices.Model.Coverlet;

public class CoverletCoverageRow
{
    public required string RowKey { get; init; }

    public required string Module { get; init; }

    public string SourceFile { get; init; } = string.Empty;

    public required string ClassName { get; init; }

    public required string MethodName { get; init; }

    public int CoveredLines { get; init; }

    public int CoverableLines { get; init; }

    public int TotalLines { get; init; }

    public decimal LineCoveragePercent { get; init; }

    public int CoveredBranches { get; init; }

    public int TotalBranches { get; init; }

    public decimal BranchCoveragePercent { get; init; }
}
