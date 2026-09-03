namespace DigitalDevServices.Model.Environments;

/// <summary>
/// In-memory snapshot of remote environment data captured on a full refresh.
/// Not persisted to SQLite — lives for the lifetime of the environment memory cache entry.
/// </summary>
public sealed record EnvironmentRefreshSnapshot
{
    public required RemoteEnvironmentDetails Details { get; init; }

    public RemoteEnvironmentDeploymentDetails? DeploymentDetails { get; init; }

    public IReadOnlyDictionary<string, RemoteBuildVersionDetails> BuildVersionDetailsByBuildNumber { get; init; }
        = new Dictionary<string, RemoteBuildVersionDetails>(StringComparer.OrdinalIgnoreCase);

    public required DateTimeOffset DateLastRefreshed { get; init; }
}
