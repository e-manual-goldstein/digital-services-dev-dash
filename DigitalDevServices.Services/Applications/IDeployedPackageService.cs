using DigitalDevServices.Model.Applications;

namespace DigitalDevServices.Services.Applications;

public interface IDeployedPackageService
{
    Task<DeployedPackageScanResult> ScanAsync(Guid applicationInstanceId, CancellationToken cancellationToken = default);
}
