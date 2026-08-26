using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IBuildVersionDetailsService
{
    Task<RemoteBuildVersionDetails?> GetBuildVersionDetailsAsync(
        string environmentPipelineBuildNumber,
        CancellationToken cancellationToken = default);
}
