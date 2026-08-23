namespace DigitalDevServices.Model.Tfs;

public static class TfsWorkItemLinkBuilder
{
    public static string? BuildWorkItemUrl(string? template, string? buildNumber)
    {
        if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(buildNumber))
        {
            return null;
        }

        return template.Replace("{BuildNumber}", buildNumber, StringComparison.Ordinal);
    }
}
