using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Model.GitHistory;

public static class ArtifactComponentDisplay
{
    public static HistoricGitRepoRecord? GetLastLocationRecord(ArtifactComponent component)
    {
        return component.PreviousLocations
            .OrderByDescending(record => record.DateMigrated)
            .ThenBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(record => !string.IsNullOrWhiteSpace(record.LastLocationUrl));
    }

    public static string? GetLastLocationUrl(ArtifactComponent component)
    {
        return GetLastLocationRecord(component)?.LastLocationUrl;
    }
}
