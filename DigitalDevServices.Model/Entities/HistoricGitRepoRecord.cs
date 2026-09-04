namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A prior home for a <see cref="GitRepository"/> before its latest migration.
/// </summary>
public class HistoricGitRepoRecord
{
    public Guid Id { get; set; }

    public Guid GitRepositoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string LastLocationUrl { get; set; } = string.Empty;

    public DateTimeOffset DateMigrated { get; set; }

    public GitRepository GitRepository { get; set; } = null!;
}
