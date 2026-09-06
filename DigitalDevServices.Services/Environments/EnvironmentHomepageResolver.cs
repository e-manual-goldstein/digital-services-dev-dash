using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public static class EnvironmentHomepageResolver
{
    public static string? SuggestHomepageUrl(
        RemoteEnvironmentDetails? environmentDetails,
        string applicationName,
        bool isWebApp)
    {
        if (!isWebApp || environmentDetails is null || string.IsNullOrWhiteSpace(applicationName))
        {
            return null;
        }

        var match = RemoteApplicationEnvironmentMatch.Find(environmentDetails, applicationName);
        var homepageUrl = match.EnvironmentUrl?.Url?.Trim();
        return string.IsNullOrWhiteSpace(homepageUrl) ? null : homepageUrl;
    }

    public static bool IsHomepageUrlManualOverride(
        string? savedHomepageUrl,
        string? suggestedHomepageUrl)
    {
        var saved = Normalize(savedHomepageUrl);
        var suggested = Normalize(suggestedHomepageUrl);

        if (saved is null && suggested is null)
        {
            return false;
        }

        if (saved is null || suggested is null)
        {
            return true;
        }

        return !string.Equals(saved, suggested, StringComparison.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
