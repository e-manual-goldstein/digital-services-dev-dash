namespace DigitalDevServices.Model.Applications;

public class ApplicationInstanceUpsert
{
    public required Guid DeployableApplicationId { get; init; }

    public required Guid EnvironmentId { get; init; }

    // (3) BuildVersionNumber — composite deployment version from pipeline output (e.g. YYMMDD.{workItemId}.0).
    // Proposed rename: BuildVersionNumber
    public required string BuildNumber { get; init; }

    public Guid? PipelineFeedId { get; init; }

    public string? SourceBranch { get; init; }

    public DateTimeOffset? DeployedAt { get; init; }

    public string? PhysicalPath { get; init; }

    public string? LogPath { get; init; }

    public string? HomepageUrl { get; init; }

    public string? SqlServerInstance { get; init; }

    public string? Notes { get; init; }
}
