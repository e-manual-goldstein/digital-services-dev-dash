using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IRemoteEnvironmentApiClient
{
    Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(int remoteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(CancellationToken cancellationToken = default);
}
