using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

/// <summary>
/// Placeholder client used when RemoteEnvironmentApi:BaseUrl is not configured.
/// </summary>
public sealed class UnconfiguredRemoteEnvironmentApiClient : IRemoteEnvironmentApiClient
{
    public Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(string environmentCode, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "RemoteEnvironmentApi:BaseUrl is not configured. Set it in appsettings to fetch environment details.");
    }

    public Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "RemoteEnvironmentApi:BaseUrl is not configured. Set it in appsettings to fetch environment details.");
    }
}
