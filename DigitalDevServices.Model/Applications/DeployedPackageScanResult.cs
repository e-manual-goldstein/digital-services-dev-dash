namespace DigitalDevServices.Model.Applications;

public class DeployedPackageScanResult
{
    public IReadOnlyList<DeployedPackageInfo> Packages { get; init; } = [];

    public DeployedPackageSource Source { get; init; } = DeployedPackageSource.FilesystemScan;

    public string? ManifestFileName { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
