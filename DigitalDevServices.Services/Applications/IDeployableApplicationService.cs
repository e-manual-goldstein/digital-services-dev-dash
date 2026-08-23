using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Services.Applications;

public interface IDeployableApplicationService
{
    Task<IReadOnlyList<DeployableApplication>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DeployableApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeployableApplication?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<DeployableApplication> CreateAsync(
        string name,
        string? projectKey = null,
        string? notes = null,
        bool isWebApp = false,
        CancellationToken cancellationToken = default);

    Task<DeployableApplication> UpdateAsync(
        Guid id,
        string name,
        string? projectKey = null,
        string? notes = null,
        bool isWebApp = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
