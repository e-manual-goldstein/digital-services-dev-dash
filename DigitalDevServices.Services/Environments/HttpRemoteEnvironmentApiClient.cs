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

    public async Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(
        string environmentCode,
        CancellationToken cancellationToken = default)
    {
        var request = new GetEnvironmentRequest
        {
            EnvironmentCode = environmentCode
        };

        var response = await _httpClient
            .PostAsJsonAsync(_options.GetEnvironmentPath, request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<RemoteEnvironmentDetails>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .GetAsync(_options.ListEnvironmentsPath, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<GetEnvironmentsResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return items?.Result ?? [];
    }
}
