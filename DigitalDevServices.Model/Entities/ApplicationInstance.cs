namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A specific deployment of a <see cref="DeployableApplication"/> in a tracked environment.
/// </summary>
public class ApplicationInstance
{
    public Guid Id { get; set; }

    public Guid DeployableApplicationId { get; set; }

    public Guid EnvironmentId { get; set; }

    // (3) BuildVersionNumber — composite deployment version from pipeline output (e.g. YYMMDD.{workItemId}.0).
    // Note: registration prefill currently copies (2) from deployment details; confirm against running code.
    // Proposed rename: BuildVersionNumber
    public string BuildNumber { get; set; } = string.Empty;

    public Guid? PipelineFeedId { get; set; }

    public string? SourceBranch { get; set; }

    public DateTimeOffset? DeployedAt { get; set; }

    public string? PhysicalPath { get; set; }

    public string? LogPath { get; set; }

    public string? HomepageUrl { get; set; }

    public string? SqlServerInstance { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DeployableApplication DeployableApplication { get; set; } = null!;

    public TrackedEnvironment Environment { get; set; } = null!;

    public PipelineFeed? PipelineFeed { get; set; }
}
