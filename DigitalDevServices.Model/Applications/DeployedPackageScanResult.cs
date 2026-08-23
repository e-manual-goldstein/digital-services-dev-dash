namespace DigitalDevServices.Model.Applications;

public class DeployedPackageScanResult
{
    public IReadOnlyList<DeployedPackageInfo> Packages { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
