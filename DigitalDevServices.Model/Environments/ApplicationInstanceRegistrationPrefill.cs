namespace DigitalDevServices.Model.Environments;

public sealed class ApplicationInstanceRegistrationPrefill
{
    public Guid? ExistingInstanceId { get; init; }

    public Guid? DeployableApplicationId { get; init; }

    // (3) BuildVersionNumber — composite deployment version from pipeline output (e.g. YYMMDD.{workItemId}.0).
    // Proposed rename: BuildVersionNumber
    public string? BuildNumber { get; init; }

    public Guid? PipelineFeedId { get; init; }

    public string? SourceBranch { get; init; }

    public DateTimeOffset? DeployedAt { get; init; }

    public string? PhysicalPath { get; init; }

    public string? LogPath { get; init; }

    public string? HomepageUrl { get; init; }

    public string? SqlServerInstance { get; init; }

    public string? Notes { get; init; }

    public ApplicationInstanceRegistrationPrefill WithDeployableApplicationId(Guid deployableApplicationId) =>
        new()
        {
            ExistingInstanceId = ExistingInstanceId,
            DeployableApplicationId = deployableApplicationId,
            BuildNumber = BuildNumber,
            PipelineFeedId = PipelineFeedId,
            SourceBranch = SourceBranch,
            DeployedAt = DeployedAt,
            PhysicalPath = PhysicalPath,
            LogPath = LogPath,
            HomepageUrl = HomepageUrl,
            SqlServerInstance = SqlServerInstance,
            Notes = Notes
        };
}
