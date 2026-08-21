using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IEnvironmentService
{
    Task<IReadOnlyList<CachedEnvironment>> GetTrackedEnvironmentsAsync(CancellationToken cancellationToken = default);

    Task<CachedEnvironment?> GetTrackedEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default);

    Task<CachedEnvironment> TrackEnvironmentAsync(int remoteId, CancellationToken cancellationToken = default);

    Task<CachedEnvironment> RefreshEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default);

    Task UntrackEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default);
}
