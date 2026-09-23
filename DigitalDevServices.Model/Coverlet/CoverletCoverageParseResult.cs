namespace DigitalDevServices.Model.Coverlet;

public class CoverletCoverageParseResult
{
    public IReadOnlyList<CoverletCoverageRow> Rows { get; init; } = [];

    public CoverletCoverageSummary? Summary { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
