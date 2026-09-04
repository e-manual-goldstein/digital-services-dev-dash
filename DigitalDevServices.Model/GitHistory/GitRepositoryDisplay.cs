using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Model.GitHistory;

public static class GitRepositoryDisplay
{
    public static string? GetLastLocationUrl(GitRepository repository)
    {
        return repository.PreviousLocations
            .OrderByDescending(record => record.DateMigrated)
            .Select(record => record.LastLocationUrl)
            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
    }
}
