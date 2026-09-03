using DigitalDevServices.Model.Applications;

namespace DigitalDevServices.Services.Applications;

public interface IDeployedPackageService
{
    Task<DeployedPackageScanResult> ScanAsync(Guid applicationInstanceId, CancellationToken cancellationToken = default);

    Task<DeployedPackageComparisonResult> CompareInstancesAsync(
        Guid leftInstanceId,
        Guid rightInstanceId,
        CancellationToken cancellationToken = default);
}
