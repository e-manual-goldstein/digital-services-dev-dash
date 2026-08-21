using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Services.Applications;

public interface IApplicationInstanceService
{
    Task<IReadOnlyList<ApplicationInstance>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ApplicationInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationInstance>> GetByEnvironmentIdAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationInstance>> GetByDeployableApplicationIdAsync(
        Guid deployableApplicationId,
        CancellationToken cancellationToken = default);

    Task<ApplicationInstance> UpsertAsync(
        ApplicationInstanceUpsert upsert,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
