using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IRemoteEnvironmentApiClient
{
    Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(
        string environmentCode,
        CancellationToken cancellationToken = default);

    Task<RemoteEnvironmentDeploymentDetails?> GetDeploymentDetailsForEnvironmentAsync(
        string environmentCode,
        CancellationToken cancellationToken = default);

    Task<RemoteBuildVersionDetails?> GetBuildVersionDetailsAsync(
        string environmentPipelineBuildNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(
        CancellationToken cancellationToken = default);
}
