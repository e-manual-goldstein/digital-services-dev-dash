using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

internal static class RemoteApplicationEnvironmentMatch
{
    public static RemoteApplicationEnvironmentMatchResult Find(
        RemoteEnvironmentDetails environmentDetails,
        string applicationName)
    {
        ArgumentNullException.ThrowIfNull(environmentDetails);

        var normalizedName = applicationName.Trim();
        EnvironmentUrl? environmentUrl = null;

        foreach (var candidate in environmentDetails.EnvironmentUrls)
        {
            if (candidate.ApplicationName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                environmentUrl = candidate;
                break;
            }
        }

        foreach (var webSite in environmentDetails.WebSites)
        {
            foreach (var webApplication in webSite.WebApplications)
            {
                if (string.Equals(
                        webApplication.ResolveDeployableApplicationName(),
                        normalizedName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new RemoteApplicationEnvironmentMatchResult
                    {
                        EnvironmentUrl = environmentUrl,
                        WebSite = webSite,
                        WebApplication = webApplication
                    };
                }
            }
        }

        foreach (var windowsService in environmentDetails.WindowsServices)
        {
            if (string.Equals(
                    windowsService.ResolveDeployableApplicationName(),
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new RemoteApplicationEnvironmentMatchResult
                {
                    EnvironmentUrl = environmentUrl,
                    WindowsService = windowsService
                };
            }
        }

        return new RemoteApplicationEnvironmentMatchResult
        {
            EnvironmentUrl = environmentUrl
        };
    }
}

internal sealed class RemoteApplicationEnvironmentMatchResult
{
    public EnvironmentUrl? EnvironmentUrl { get; init; }

    public EnvironmentWebSite? WebSite { get; init; }

    public EnvironmentWebApplication? WebApplication { get; init; }

    public EnvironmentWindowsService? WindowsService { get; init; }

    public string? GetMachineName(RemoteEnvironmentDetails environmentDetails)
    {
        if (!string.IsNullOrWhiteSpace(WebSite?.MachineName))
        {
            return WebSite.MachineName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(WindowsService?.MachineName))
        {
            return WindowsService.MachineName.Trim();
        }

        if (EnvironmentUrl is not null)
        {
            return environmentDetails.WebSites
                .FirstOrDefault(site => !string.IsNullOrWhiteSpace(site.MachineName))
                ?.MachineName?.Trim();
        }

        return null;
    }

    public EnvironmentWebSite? GetTemplateContextWebSite(RemoteEnvironmentDetails environmentDetails)
    {
        if (WebSite is not null)
        {
            return WebSite;
        }

        if (EnvironmentUrl is null)
        {
            return null;
        }

        return environmentDetails.WebSites.FirstOrDefault(site => !string.IsNullOrWhiteSpace(site.MachineName));
    }
}
