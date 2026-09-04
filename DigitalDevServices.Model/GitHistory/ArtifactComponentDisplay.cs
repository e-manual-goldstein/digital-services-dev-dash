using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Model.GitHistory;

public static class ArtifactComponentDisplay
{
    public static string? GetLastLocationUrl(ArtifactComponent component)
    {
        return component.PreviousLocations
            .OrderByDescending(record => record.DateMigrated)
            .Select(record => record.LastLocationUrl)
            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
    }
}
