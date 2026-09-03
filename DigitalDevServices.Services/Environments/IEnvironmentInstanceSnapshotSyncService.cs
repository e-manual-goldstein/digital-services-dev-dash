using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IEnvironmentInstanceSnapshotSyncService
{
    Task<int> SyncInstancesAsync(
        Guid environmentLocalId,
        EnvironmentRefreshSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
