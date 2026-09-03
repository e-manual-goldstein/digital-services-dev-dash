namespace DigitalDevServices.Model.Applications;

public class DeployedPackageComparisonRow
{
    public required string FileName { get; init; }

    public string? LeftVersion { get; init; }

    public string? RightVersion { get; init; }

    public DeployedPackageComparisonStatus Status { get; init; }
}
