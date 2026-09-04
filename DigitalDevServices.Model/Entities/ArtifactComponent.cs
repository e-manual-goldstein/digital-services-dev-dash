namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A logical component within a <see cref="GitRepository"/> with its own migration history.
/// </summary>
public class ArtifactComponent
{
    public Guid Id { get; set; }

    public Guid GitRepositoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset DateMigrated { get; set; }

    public string CurrentLocationUrl { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public GitRepository GitRepository { get; set; } = null!;

    public ICollection<HistoricGitRepoRecord> PreviousLocations { get; set; } = [];
}
