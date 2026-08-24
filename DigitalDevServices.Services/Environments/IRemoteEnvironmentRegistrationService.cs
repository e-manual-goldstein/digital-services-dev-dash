using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IRemoteEnvironmentRegistrationService
{
    Task<ApplicationInstance> RegisterFromEnvironmentUrlAsync(
        Guid environmentId,
        EnvironmentUrl environmentUrl,
        CancellationToken cancellationToken = default);

    Task<ApplicationInstance> RegisterFromWebApplicationAsync(
        Guid environmentId,
        EnvironmentWebApplication webApplication,
        CancellationToken cancellationToken = default);
}
