namespace DigitalDevServices.Model.Coverlet;

public class CoverletModuleSummaryRow
{
    public required string ModuleName { get; init; }

    public int ClassCount { get; init; }

    public int MethodCount { get; init; }

    public decimal LineCoveragePercent { get; init; }
}
