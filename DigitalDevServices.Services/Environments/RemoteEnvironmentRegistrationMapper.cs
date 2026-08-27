using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Environments;

public sealed class RemoteEnvironmentRegistrationMapper : IRemoteEnvironmentRegistrationMapper
{
    private readonly IDeployableApplicationService _deployableApplicationService;
    private readonly IApplicationInstanceService _applicationInstanceService;
    private readonly ILogPathTemplateService _logPathTemplateService;

    public RemoteEnvironmentRegistrationMapper(
        IDeployableApplicationService deployableApplicationService,
        IApplicationInstanceService applicationInstanceService,
        ILogPathTemplateService logPathTemplateService)
    {
        _deployableApplicationService = deployableApplicationService;
        _applicationInstanceService = applicationInstanceService;
        _logPathTemplateService = logPathTemplateService;
    }

    public async Task<RemoteRegistrationPrefill> BuildFromEnvironmentUrlAsync(
        Guid environmentId,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentUrl environmentUrl,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environmentUrl);
        ArgumentNullException.ThrowIfNull(environmentDetails);

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
        var existingInstance = await FindExistingInstanceAsync(
            environmentId,
            deployableApplication?.Id,
            cancellationToken).ConfigureAwait(false);

        DeployableApplicationRegistrationPrefill? applicationPrefill = deployableApplication is null
            ? new DeployableApplicationRegistrationPrefill
            {
                Name = applicationName,
                IsWebApp = true
            }
            : null;

        var remoteMatch = RemoteApplicationEnvironmentMatch.Find(environmentDetails, applicationName);

        var instancePrefill = BuildInstancePrefill(
            deployableApplication,
            existingInstance,
            environmentDetails,
            remoteMatch.GetTemplateContextWebSite(environmentDetails),
            remoteMatch.WebApplication,
            deploymentDetails,
            applicationName,
            homepageUrl: homepageUrl,
            userPhysicalPathOverride: null,
            remotePhysicalPath: remoteMatch.WebApplication?.PhysicalPath?.Trim() ?? existingInstance?.PhysicalPath,
            machineName: remoteMatch.GetMachineName(environmentDetails));

        return new RemoteRegistrationPrefill
        {
            EnvironmentLocalId = environmentId,
            Source = RemoteRegistrationSource.EnvironmentUrl,
            Application = applicationPrefill,
            Instance = instancePrefill
        };
    }

    public async Task<RemoteRegistrationPrefill> BuildFromWebApplicationAsync(
        Guid environmentId,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWebSite webSite,
        EnvironmentWebApplication webApplication,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webSite);
        ArgumentNullException.ThrowIfNull(webApplication);
        ArgumentNullException.ThrowIfNull(environmentDetails);

        var applicationName = webApplication.ResolveDeployableApplicationName()
            ?? throw new ArgumentException("Application pool name or path is required.", nameof(webApplication));

        var deployableApplication = await _deployableApplicationService
            .GetByNameAsync(applicationName, cancellationToken)
            .ConfigureAwait(false);
        var existingInstance = await FindExistingInstanceAsync(
            environmentId,
            deployableApplication?.Id,
            cancellationToken).ConfigureAwait(false);

        DeployableApplicationRegistrationPrefill? applicationPrefill = deployableApplication is null
            ? new DeployableApplicationRegistrationPrefill
            {
                Name = applicationName,
                IsWebApp = true
            }
            : null;

        var instancePrefill = BuildInstancePrefill(
            deployableApplication,
            existingInstance,
            environmentDetails,
            webSite,
            webApplication,
            deploymentDetails,
            applicationName,
            homepageUrl: existingInstance?.HomepageUrl,
            userPhysicalPathOverride: null,
            remotePhysicalPath: webApplication.PhysicalPath?.Trim() ?? existingInstance?.PhysicalPath,
            machineName: webSite.MachineName);

        return new RemoteRegistrationPrefill
        {
            EnvironmentLocalId = environmentId,
            Source = RemoteRegistrationSource.WebApplication,
            Application = applicationPrefill,
            Instance = instancePrefill
        };
    }

    public async Task<RemoteRegistrationPrefill> BuildFromWindowsServiceAsync(
        Guid environmentId,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWindowsService windowsService,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(windowsService);
        ArgumentNullException.ThrowIfNull(environmentDetails);

        var applicationName = windowsService.ResolveDeployableApplicationName()
            ?? throw new ArgumentException("Display name or binary path is required.", nameof(windowsService));
        var binaryPath = windowsService.BinaryPathName?.Trim()
            ?? throw new ArgumentException("Binary path is required.", nameof(windowsService));

        if (binaryPath.Length == 0)
        {
            throw new ArgumentException("Binary path is required.", nameof(windowsService));
        }

        var deployableApplication = await _deployableApplicationService
            .GetByNameAsync(applicationName, cancellationToken)
            .ConfigureAwait(false);
        var existingInstance = await FindExistingInstanceAsync(
            environmentId,
            deployableApplication?.Id,
            cancellationToken).ConfigureAwait(false);

        DeployableApplicationRegistrationPrefill? applicationPrefill = deployableApplication is null
            ? new DeployableApplicationRegistrationPrefill
            {
                Name = applicationName,
                IsWebApp = false
            }
            : null;

        var instancePrefill = BuildWindowsServiceInstancePrefill(
            deployableApplication,
            existingInstance,
            environmentDetails,
            windowsService,
            deploymentDetails,
            applicationName,
            binaryPath);

        return new RemoteRegistrationPrefill
        {
            EnvironmentLocalId = environmentId,
            Source = RemoteRegistrationSource.WindowsService,
            Application = applicationPrefill,
            Instance = instancePrefill
        };
    }

    public ApplicationInstanceRegistrationPrefill BuildManualDeploymentPrefill(
        RemoteEnvironmentDetails environmentDetails,
        DeployableApplication deployableApplication,
        RemoteEnvironmentDeploymentDetails? deploymentDetails = null,
        string? physicalPath = null)
    {
        ArgumentNullException.ThrowIfNull(environmentDetails);
        ArgumentNullException.ThrowIfNull(deployableApplication);

        var remoteMatch = RemoteApplicationEnvironmentMatch.Find(environmentDetails, deployableApplication.Name);
        var userPhysicalPathOverride = string.IsNullOrWhiteSpace(physicalPath) ? null : physicalPath.Trim();

        return BuildInstancePrefill(
            deployableApplication,
            existingInstance: null,
            environmentDetails,
            remoteMatch.GetTemplateContextWebSite(environmentDetails),
            remoteMatch.WebApplication,
            deploymentDetails,
            deployableApplication.Name,
            homepageUrl: remoteMatch.EnvironmentUrl?.Url?.Trim(),
            userPhysicalPathOverride: userPhysicalPathOverride,
            remotePhysicalPath: remoteMatch.WebApplication?.PhysicalPath?.Trim()
                ?? remoteMatch.WindowsService?.BinaryPathName?.Trim(),
            machineName: remoteMatch.GetMachineName(environmentDetails));
    }

    private async Task<ApplicationInstance?> FindExistingInstanceAsync(
        Guid environmentId,
        Guid? deployableApplicationId,
        CancellationToken cancellationToken)
    {
        if (deployableApplicationId is null)
        {
            return null;
        }

        var instances = await _applicationInstanceService
            .GetByEnvironmentIdAsync(environmentId, cancellationToken)
            .ConfigureAwait(false);

        return instances.SingleOrDefault(instance => instance.DeployableApplicationId == deployableApplicationId);
    }

    private ApplicationInstanceRegistrationPrefill BuildInstancePrefill(
        DeployableApplication? deployableApplication,
        ApplicationInstance? existingInstance,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWebSite? webSite,
        EnvironmentWebApplication? webApplication,
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string? applicationName,
        string? homepageUrl,
        string? userPhysicalPathOverride,
        string? remotePhysicalPath,
        string? machineName = null)
    {
        var templateContext = BuildTemplateContext(
            deployableApplication,
            environmentDetails,
            webSite,
            webApplication,
            applicationName,
            remotePhysicalPath,
            machineName);

        var resolvedPhysicalPath = !string.IsNullOrWhiteSpace(userPhysicalPathOverride)
            ? userPhysicalPathOverride.Trim()
            : TryResolvePhysicalPath(deployableApplication, templateContext)
                ?? remotePhysicalPath?.Trim()
                ?? existingInstance?.PhysicalPath;

        var logPathContext = BuildTemplateContext(
            deployableApplication,
            environmentDetails,
            webSite,
            webApplication,
            applicationName,
            resolvedPhysicalPath,
            machineName);
        var resolvedLogPath = TryResolveLogPath(deployableApplication, logPathContext);

        return new ApplicationInstanceRegistrationPrefill
        {
            ExistingInstanceId = existingInstance?.Id,
            DeployableApplicationId = deployableApplication?.Id,
            BuildVersionNumber = existingInstance?.BuildVersionNumber
                ?? GetSuggestedBuildNumber(deploymentDetails, applicationName),
            PipelineFeedId = existingInstance?.PipelineFeedId,
            SourceBranch = existingInstance?.SourceBranch
                ?? GetSuggestedSourceBranch(deploymentDetails, applicationName),
            DeployedAt = existingInstance?.DeployedAt,
            PhysicalPath = resolvedPhysicalPath,
            LogPath = resolvedLogPath ?? existingInstance?.LogPath,
            HomepageUrl = homepageUrl,
            SqlServerInstance = existingInstance?.SqlServerInstance,
            Notes = existingInstance?.Notes
        };
    }

    private ApplicationInstanceRegistrationPrefill BuildWindowsServiceInstancePrefill(
        DeployableApplication? deployableApplication,
        ApplicationInstance? existingInstance,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWindowsService windowsService,
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string applicationName,
        string binaryPath)
    {
        var templateContext = BuildWindowsServiceTemplateContext(
            deployableApplication,
            environmentDetails,
            windowsService,
            applicationName,
            binaryPath);

        var resolvedPhysicalPath = TryResolvePhysicalPath(deployableApplication, templateContext)
            ?? binaryPath
            ?? existingInstance?.PhysicalPath;

        var logPathContext = BuildWindowsServiceTemplateContext(
            deployableApplication,
            environmentDetails,
            windowsService,
            applicationName,
            resolvedPhysicalPath);
        var resolvedLogPath = TryResolveLogPath(deployableApplication, logPathContext);

        return new ApplicationInstanceRegistrationPrefill
        {
            ExistingInstanceId = existingInstance?.Id,
            DeployableApplicationId = deployableApplication?.Id,
            BuildVersionNumber = existingInstance?.BuildVersionNumber
                ?? GetSuggestedBuildNumber(deploymentDetails, applicationName),
            PipelineFeedId = existingInstance?.PipelineFeedId,
            SourceBranch = existingInstance?.SourceBranch
                ?? GetSuggestedSourceBranch(deploymentDetails, applicationName),
            DeployedAt = existingInstance?.DeployedAt,
            PhysicalPath = resolvedPhysicalPath,
            LogPath = resolvedLogPath ?? existingInstance?.LogPath,
            HomepageUrl = existingInstance?.HomepageUrl,
            SqlServerInstance = existingInstance?.SqlServerInstance,
            Notes = existingInstance?.Notes
        };
    }

    private static LogPathTemplateContext BuildWindowsServiceTemplateContext(
        DeployableApplication? deployableApplication,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWindowsService windowsService,
        string? applicationName,
        string? physicalPath) =>
        new()
        {
            AppName = deployableApplication?.Name ?? applicationName,
            EnvironmentCode = environmentDetails.Code,
            EnvironmentName = environmentDetails.Name,
            MachineName = windowsService.MachineName,
            PhysicalPath = physicalPath
        };

    private static string GetSuggestedBuildNumber(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string? applicationName) =>
        deploymentDetails?.GetBuildNumberForApplication(applicationName ?? string.Empty)
        ?? RemoteEnvironmentRegistrationDefaults.BuildNumber;

    private static string? GetSuggestedSourceBranch(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        string? applicationName)
    {
        if (deploymentDetails is null || string.IsNullOrWhiteSpace(applicationName))
        {
            return deploymentDetails?.GetPrimaryWipBranch();
        }

        return deploymentDetails.GetWipBranchForApplication(applicationName);
    }

    private static LogPathTemplateContext BuildTemplateContext(
        DeployableApplication? deployableApplication,
        RemoteEnvironmentDetails environmentDetails,
        EnvironmentWebSite? webSite,
        EnvironmentWebApplication? webApplication,
        string? applicationName,
        string? physicalPath,
        string? machineName = null) =>
        new()
        {
            AppName = deployableApplication?.Name ?? applicationName,
            EnvironmentCode = environmentDetails.Code,
            EnvironmentName = environmentDetails.Name,
            MachineName = machineName ?? webSite?.MachineName,
            ApplicationPoolName = webApplication?.ApplicationPoolName,
            VirtualPath = webApplication?.Path,
            PhysicalPath = physicalPath
        };

    private string? TryResolvePhysicalPath(
        DeployableApplication? deployableApplication,
        LogPathTemplateContext context)
    {
        if (deployableApplication?.PathToPhysicalPath is not { Length: > 0 } template)
        {
            return null;
        }

        return TryResolveTemplate(template, context);
    }

    private string? TryResolveLogPath(
        DeployableApplication? deployableApplication,
        LogPathTemplateContext context)
    {
        if (deployableApplication?.PathToLogFiles is not { Length: > 0 } template)
        {
            return null;
        }

        return TryResolveTemplate(template, context);
    }

    private string? TryResolveTemplate(string template, LogPathTemplateContext context)
    {
        var result = _logPathTemplateService.Resolve(template, context);
        return result.UnknownTokens.Count == 0 && !string.IsNullOrWhiteSpace(result.ResolvedPath)
            ? result.ResolvedPath
            : null;
    }
}
