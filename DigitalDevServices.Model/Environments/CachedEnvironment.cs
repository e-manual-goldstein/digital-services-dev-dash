namespace DigitalDevServices.Model.Environments;

/// <summary>
/// A tracked environment combined with cached remote API details.
/// </summary>
public record CachedEnvironment
{
    public required Guid LocalId { get; init; }

    public required int RemoteId { get; init; }

    public required bool IsFavourite { get; init; }

    public required RemoteEnvironmentDetails Details { get; init; }

    public RemoteEnvironmentDeploymentDetails? DeploymentDetails { get; init; }

    /// <summary>
    /// Full refresh snapshot (details, deployment builds, build version metadata). Null until the environment is refreshed.
    /// </summary>
    public EnvironmentRefreshSnapshot? RefreshSnapshot { get; init; }

    public required DateTimeOffset DateLastUpdated { get; init; }

    public DateTimeOffset DateLastRefreshed => RefreshSnapshot?.DateLastRefreshed ?? DateLastUpdated;

    public required bool IsFromCache { get; init; }

    public bool HasRefreshSnapshot => RefreshSnapshot is not null;
}
