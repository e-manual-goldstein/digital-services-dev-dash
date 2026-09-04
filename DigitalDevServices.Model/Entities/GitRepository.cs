namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A Git repository tracked for migration history across Azure DevOps repos.
/// </summary>
public class GitRepository
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset DateMigrated { get; set; }

    public string CurrentLocationUrl { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<HistoricGitRepoRecord> PreviousLocations { get; set; } = [];
}
