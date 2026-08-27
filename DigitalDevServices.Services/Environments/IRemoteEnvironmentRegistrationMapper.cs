using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IRemoteEnvironmentRegistrationMapper
{
    Task<RemoteRegistrationPrefill> BuildFromEnvironmentUrlAsync(
        Guid environmentId,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentUrl environmentUrl,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        CancellationToken cancellationToken = default);

    Task<RemoteRegistrationPrefill> BuildFromWebApplicationAsync(
        Guid environmentId,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWebSite webSite,
        EnvironmentWebApplication webApplication,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        CancellationToken cancellationToken = default);

    Task<RemoteRegistrationPrefill> BuildFromWindowsServiceAsync(
        Guid environmentId,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWindowsService windowsService,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        CancellationToken cancellationToken = default);

    ApplicationInstanceRegistrationPrefill BuildManualDeploymentPrefill(
        RemoteEnvironmentDetails environmentDetails,
        DeployableApplication deployableApplication,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        string? physicalPath = null);
}
