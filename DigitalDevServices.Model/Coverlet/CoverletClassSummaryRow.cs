namespace DigitalDevServices.Model.Coverlet;

public class CoverletClassSummaryRow
{
    public required string ClassName { get; init; }

    public int MethodCount { get; init; }

    public decimal LineCoveragePercent { get; init; }
}
