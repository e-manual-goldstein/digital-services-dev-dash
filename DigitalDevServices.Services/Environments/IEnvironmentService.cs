using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IEnvironmentService
{
    /// <summary>
    /// Loads all environments from the remote Web API (cached) and ensures local tracking records exist.
    /// </summary>
    Task<IReadOnlyList<CachedEnvironment>> GetEnvironmentsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    Task<CachedEnvironment?> GetTrackedEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default);

    Task<CachedEnvironment> RefreshEnvironmentAsync(int remoteId, CancellationToken cancellationToken = default);

    Task UntrackEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default);
}
