using System.Net.Http.Json;
using DigitalDevServices.Model.Environments;
using Microsoft.Extensions.Options;

namespace DigitalDevServices.Services.Environments;

public sealed class HttpRemoteEnvironmentApiClient : IRemoteEnvironmentApiClient
{
    private readonly HttpClient _httpClient;
    private readonly RemoteEnvironmentApiOptions _options;

    public HttpRemoteEnvironmentApiClient(HttpClient httpClient, IOptions<RemoteEnvironmentApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(int remoteId, CancellationToken cancellationToken = default)
    {
        var path = _options.GetEnvironmentPath.Replace("{id}", remoteId.ToString(), StringComparison.Ordinal);
        var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var details = await response.Content.ReadFromJsonAsync<RemoteEnvironmentDetails>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (details is not null)
        {
            details.RemoteId = remoteId;
        }

        return details;
    }

    public async Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(_options.ListEnvironmentsPath, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<RemoteEnvironmentDetails>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return items ?? [];
    }
}
