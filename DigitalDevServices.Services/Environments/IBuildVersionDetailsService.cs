using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IBuildVersionDetailsService
{
    Task<RemoteBuildVersionDetails?> GetBuildVersionDetailsAsync(
        int buildNumber,
        CancellationToken cancellationToken = default);
}
