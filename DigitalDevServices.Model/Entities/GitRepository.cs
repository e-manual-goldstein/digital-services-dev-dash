namespace DigitalDevServices.Model.Entities;

/// <summary>
/// An Azure DevOps git repository that may contain multiple artifact components migrated at different times.
/// </summary>
public class GitRepository
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ArtifactComponent> ArtifactComponents { get; set; } = [];
}
