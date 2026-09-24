namespace DigitalDevServices.Model.Coverlet;

public class CoverletClassSummaryRow
{
    public required string ClassName { get; init; }

    public int MethodCount { get; init; }

    /// <summary>Sum of <see cref="CoverletCoverageRow.CoverableLines"/> across methods in the class.</summary>
    public int LineCount { get; init; }

    public decimal LineCoveragePercent { get; init; }
}
