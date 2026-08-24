using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Environments;

public sealed class RemoteEnvironmentRegistrationService : IRemoteEnvironmentRegistrationService
{
    private readonly IDeployableApplicationService _deployableApplicationService;
    private readonly IApplicationInstanceService _applicationInstanceService;

    public RemoteEnvironmentRegistrationService(
        IDeployableApplicationService deployableApplicationService,
        IApplicationInstanceService applicationInstanceService)
    {
        _deployableApplicationService = deployableApplicationService;
        _applicationInstanceService = applicationInstanceService;
    }

    public async Task<ApplicationInstance> RegisterFromEnvironmentUrlAsync(
        Guid environmentId,
        EnvironmentUrl environmentUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environmentUrl);

        var applicationName = environmentUrl.ApplicationName?.Trim()
            ?? throw new ArgumentException("Application name is required.", nameof(environmentUrl));
        var homepageUrl = environmentUrl.Url?.Trim()
            ?? throw new ArgumentException("URL is required.", nameof(environmentUrl));

        if (homepageUrl.Length == 0)
        {
            throw new ArgumentException("URL is required.", nameof(environmentUrl));
        }

        var deployableApplication = await _deployableApplicationService
            .GetByNameAsync(applicationName, cancellationToken)
            .ConfigureAwait(false);

        if (deployableApplication is null)
        {
            deployableApplication = await _deployableApplicationService
                .CreateAsync(applicationName, isWebApp: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else if (!deployableApplication.IsWebApp)
        {
            deployableApplication = await _deployableApplicationService.UpdateAsync(
                deployableApplication.Id,
                deployableApplication.Name,
                deployableApplication.ProjectKey,
                deployableApplication.Notes,
                isWebApp: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var existingInstances = await _applicationInstanceService
            .GetByEnvironmentIdAsync(environmentId, cancellationToken)
            .ConfigureAwait(false);
        var existing = existingInstances.SingleOrDefault(instance =>
            instance.DeployableApplicationId == deployableApplication.Id);

        return await _applicationInstanceService.UpsertAsync(
            new ApplicationInstanceUpsert
            {
                DeployableApplicationId = deployableApplication.Id,
                EnvironmentId = environmentId,
                BuildNumber = existing?.BuildNumber ?? RemoteEnvironmentRegistrationDefaults.BuildNumber,
                PipelineFeedId = existing?.PipelineFeedId,
                SourceBranch = existing?.SourceBranch,
                DeployedAt = existing?.DeployedAt,
                PhysicalPath = existing?.PhysicalPath,
                LogPath = existing?.LogPath,
                HomepageUrl = homepageUrl,
                SqlServerInstance = existing?.SqlServerInstance,
                Notes = existing?.Notes
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApplicationInstance> RegisterFromWebApplicationAsync(
        Guid environmentId,
        EnvironmentWebApplication webApplication,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webApplication);

        var applicationName = webApplication.ResolveDeployableApplicationName()
            ?? throw new ArgumentException("Application pool name or path is required.", nameof(webApplication));
        var physicalPath = webApplication.PhysicalPath?.Trim()
            ?? throw new ArgumentException("Physical path is required.", nameof(webApplication));

        if (physicalPath.Length == 0)
        {
            throw new ArgumentException("Physical path is required.", nameof(webApplication));
        }

        var deployableApplication = await _deployableApplicationService
            .GetByNameAsync(applicationName, cancellationToken)
            .ConfigureAwait(false);

        if (deployableApplication is null)
        {
            deployableApplication = await _deployableApplicationService
                .CreateAsync(applicationName, isWebApp: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else if (!deployableApplication.IsWebApp)
        {
            deployableApplication = await _deployableApplicationService.UpdateAsync(
                deployableApplication.Id,
                deployableApplication.Name,
                deployableApplication.ProjectKey,
                deployableApplication.Notes,
                isWebApp: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var existingInstances = await _applicationInstanceService
            .GetByEnvironmentIdAsync(environmentId, cancellationToken)
            .ConfigureAwait(false);
        var existing = existingInstances.SingleOrDefault(instance =>
            instance.DeployableApplicationId == deployableApplication.Id);

        return await _applicationInstanceService.UpsertAsync(
            new ApplicationInstanceUpsert
            {
                DeployableApplicationId = deployableApplication.Id,
                EnvironmentId = environmentId,
                BuildNumber = existing?.BuildNumber ?? RemoteEnvironmentRegistrationDefaults.BuildNumber,
                PipelineFeedId = existing?.PipelineFeedId,
                SourceBranch = existing?.SourceBranch,
                DeployedAt = existing?.DeployedAt,
                PhysicalPath = physicalPath,
                LogPath = existing?.LogPath,
                HomepageUrl = existing?.HomepageUrl,
                SqlServerInstance = existing?.SqlServerInstance,
                Notes = existing?.Notes
            },
            cancellationToken).ConfigureAwait(false);
    }
}
