namespace DigitalDevServices.Model.Applications;

public class ApplicationInstanceUpsert
{
    public required Guid DeployableApplicationId { get; init; }

    public required Guid EnvironmentId { get; init; }

    public required string BuildNumber { get; init; }

    public Guid? PipelineFeedId { get; init; }

    public string? SourceBranch { get; init; }

    public DateTimeOffset? DeployedAt { get; init; }

    public string? PhysicalPath { get; init; }

    public string? LogPath { get; init; }

    public string? SqlServerInstance { get; init; }

    public string? Notes { get; init; }
}
