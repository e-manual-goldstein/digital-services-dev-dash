using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Environments;

namespace DigitalDevServices.Services.Logs;

public sealed class LogPathResolutionService : ILogPathResolutionService
{
    private readonly IApplicationInstanceService _applicationInstanceService;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogPathTemplateService _logPathTemplateService;

    public LogPathResolutionService(
        IApplicationInstanceService applicationInstanceService,
        IEnvironmentService environmentService,
        ILogPathTemplateService logPathTemplateService)
    {
        _applicationInstanceService = applicationInstanceService;
        _environmentService = environmentService;
        _logPathTemplateService = logPathTemplateService;
    }

    public async Task<LogPathResolutionResult> EnsureLogPathAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await _applicationInstanceService
            .GetByIdAsync(applicationInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            return new LogPathResolutionResult
            {
                ErrorMessage = "Application instance was not found."
            };
        }

        if (!string.IsNullOrWhiteSpace(instance.LogPath))
        {
            return new LogPathResolutionResult
            {
                IsSuccess = true,
                LogPath = instance.LogPath.Trim()
            };
        }

        var template = instance.DeployableApplication.PathToLogFiles;
        if (string.IsNullOrWhiteSpace(template))
        {
            return new LogPathResolutionResult
            {
                ErrorMessage =
                    "Log path is not set on this deployment and the deployable application has no PathToLogFiles template. " +
                    "Configure a template on the application or set Log path on the deployment."
            };
        }

        var cachedEnvironment = await _environmentService
            .GetTrackedEnvironmentAsync(instance.EnvironmentId, cancellationToken)
            .ConfigureAwait(false);

        if (cachedEnvironment is null)
        {
            return new LogPathResolutionResult
            {
                ErrorMessage = "Tracked environment was not found for this deployment."
            };
        }

        var refreshedEnvironment = false;
        var resolution = TryResolveLogPath(instance, cachedEnvironment.Details, template);
        if (!resolution.IsSuccess)
        {
            cachedEnvironment = await _environmentService
                .RefreshEnvironmentAsync(cachedEnvironment.RemoteId, cancellationToken)
                .ConfigureAwait(false);
            refreshedEnvironment = true;
            resolution = TryResolveLogPath(instance, cachedEnvironment.Details, template);
        }

        if (!resolution.IsSuccess)
        {
            var tokenSummary = resolution.UnknownTokens.Count > 0
                ? $" Missing template tokens: {string.Join(", ", resolution.UnknownTokens.Select(token => $"{{{token}}}"))}."
                : string.Empty;

            return new LogPathResolutionResult
            {
                RefreshedEnvironment = refreshedEnvironment,
                UnknownTokens = resolution.UnknownTokens,
                ErrorMessage =
                    "Could not resolve the log path from the deployable application template and cached environment data." +
                    tokenSummary
            };
        }

        await _applicationInstanceService.UpsertAsync(
            ToUpsert(instance, resolution.LogPath!),
            cancellationToken).ConfigureAwait(false);

        return new LogPathResolutionResult
        {
            IsSuccess = true,
            LogPath = resolution.LogPath,
            RefreshedEnvironment = refreshedEnvironment
        };
    }

    private (bool IsSuccess, string? LogPath, IReadOnlyList<string> UnknownTokens) TryResolveLogPath(
        ApplicationInstance instance,
        RemoteEnvironmentDetails environmentDetails,
        string template)
    {
        var remoteMatch = RemoteApplicationEnvironmentMatch.Find(
            environmentDetails,
            instance.DeployableApplication.Name);

        var context = new LogPathTemplateContext
        {
            AppName = instance.DeployableApplication.Name,
            EnvironmentCode = environmentDetails.Code,
            EnvironmentName = environmentDetails.Name,
            MachineName = remoteMatch.GetMachineName(environmentDetails),
            ApplicationPoolName = remoteMatch.WebApplication?.ApplicationPoolName,
            VirtualPath = remoteMatch.WebApplication?.Path,
            PhysicalPath = instance.PhysicalPath
        };

        var missingTokens = GetMissingTemplateTokens(template, context);
        if (missingTokens.Count > 0)
        {
            return (false, null, missingTokens);
        }

        var result = _logPathTemplateService.Resolve(template, context);
        if (result.UnknownTokens.Count > 0 || string.IsNullOrWhiteSpace(result.ResolvedPath))
        {
            return (false, null, result.UnknownTokens);
        }

        return (true, result.ResolvedPath, []);
    }

    private static IReadOnlyList<string> GetMissingTemplateTokens(string template, LogPathTemplateContext context)
    {
        var missing = new List<string>();

        foreach (var token in LogPathTemplateTokens.All)
        {
            if (!template.Contains($"{{{token}}}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(GetContextValue(token, context)))
            {
                missing.Add(token);
            }
        }

        return missing;
    }

    private static string? GetContextValue(string tokenName, LogPathTemplateContext context) =>
        tokenName switch
        {
            LogPathTemplateTokens.AppName => context.AppName,
            LogPathTemplateTokens.EnvironmentCode => context.EnvironmentCode,
            LogPathTemplateTokens.EnvironmentName => context.EnvironmentName,
            LogPathTemplateTokens.MachineName => context.MachineName,
            LogPathTemplateTokens.ApplicationPoolName => context.ApplicationPoolName,
            LogPathTemplateTokens.VirtualPath => context.VirtualPath,
            LogPathTemplateTokens.PhysicalPath => context.PhysicalPath,
            _ => null
        };

    private static ApplicationInstanceUpsert ToUpsert(ApplicationInstance instance, string logPath) =>
        new()
        {
            DeployableApplicationId = instance.DeployableApplicationId,
            EnvironmentId = instance.EnvironmentId,
            BuildVersionNumber = instance.BuildVersionNumber,
            PipelineFeedId = instance.PipelineFeedId,
            SourceBranch = instance.SourceBranch,
            DeployedAt = instance.DeployedAt,
            PhysicalPath = instance.PhysicalPath,
            LogPath = logPath,
            HomepageUrl = instance.HomepageUrl,
            SqlServerInstance = instance.SqlServerInstance,
            Notes = instance.Notes
        };
}
