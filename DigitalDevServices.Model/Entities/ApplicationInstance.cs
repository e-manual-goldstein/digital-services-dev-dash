namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A specific deployment of a <see cref="DeployableApplication"/> in a tracked environment.
/// </summary>
public class ApplicationInstance
{
    public Guid Id { get; set; }

    public Guid DeployableApplicationId { get; set; }

    public Guid EnvironmentId { get; set; }

    public string BuildNumber { get; set; } = string.Empty;

    public Guid? PipelineFeedId { get; set; }

    public string? SourceBranch { get; set; }

    public DateTimeOffset? DeployedAt { get; set; }

    public string? PhysicalPath { get; set; }

    public string? LogPath { get; set; }

    public string? SqlServerInstance { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DeployableApplication DeployableApplication { get; set; } = null!;

    public TrackedEnvironment Environment { get; set; } = null!;

    public PipelineFeed? PipelineFeed { get; set; }
}
