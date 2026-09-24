namespace DigitalDevServices.Model.Coverlet;

public class CoverletCoverageExportDocument
{
    public DateTimeOffset ExportedAt { get; init; }

    public string? SourceFileName { get; init; }

    public int RowCount { get; init; }

    public IReadOnlyList<CoverletCoverageExportRow> Rows { get; init; } = [];
}

public class CoverletCoverageExportRow
{
    public required string Module { get; init; }

    public string SourceFile { get; init; } = string.Empty;

    public required string ClassName { get; init; }

    public required string MethodName { get; init; }

    public int CoveredLines { get; init; }

    public int CoverableLines { get; init; }

    public decimal LineCoveragePercent { get; init; }

    public int CoveredBranches { get; init; }

    public int TotalBranches { get; init; }

    public decimal BranchCoveragePercent { get; init; }
}
