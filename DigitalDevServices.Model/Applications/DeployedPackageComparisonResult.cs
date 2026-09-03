namespace DigitalDevServices.Model.Applications;

public class DeployedPackageComparisonResult
{
    public Guid LeftInstanceId { get; init; }

    public Guid RightInstanceId { get; init; }

    public string LeftInstanceLabel { get; init; } = string.Empty;

    public string RightInstanceLabel { get; init; } = string.Empty;

    public DeployedPackageScanResult? LeftScan { get; init; }

    public DeployedPackageScanResult? RightScan { get; init; }

    public IReadOnlyList<DeployedPackageComparisonRow> Rows { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
