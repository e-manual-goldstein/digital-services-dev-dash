namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A prior home for an <see cref="ArtifactComponent"/> before its latest migration.
/// </summary>
public class HistoricGitRepoRecord
{
    public Guid Id { get; set; }

    public Guid ArtifactComponentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string LastLocationUrl { get; set; } = string.Empty;

    public DateTimeOffset DateMigrated { get; set; }

    public ArtifactComponent ArtifactComponent { get; set; } = null!;
}
