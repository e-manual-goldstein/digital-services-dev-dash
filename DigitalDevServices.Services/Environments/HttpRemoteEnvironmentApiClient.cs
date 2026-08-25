using System.Net.Http.Json;
using DigitalDevServices.Model;
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
        var wrapped = await response.Content
            .ReadFromJsonAsync<RemoteApiResponse<RemoteEnvironmentDetails>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return wrapped?.Result;
    }

    public async Task<RemoteEnvironmentDeploymentDetails?> GetDeploymentDetailsForEnvironmentAsync(
        string environmentCode,
        CancellationToken cancellationToken = default)
    {
        var request = new GetEnvironmentRequest
        {
            EnvironmentCode = environmentCode
        };

        var response = await _httpClient
            .PostAsJsonAsync(_options.GetDeploymentDetailsForEnvironmentPath, request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var wrapped = await response.Content
            .ReadFromJsonAsync<RemoteApiResponse<RemoteEnvironmentDeploymentDetails>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return wrapped?.Result;
    }

    public async Task<RemoteBuildVersionDetails?> GetBuildVersionDetailsAsync(
        int buildNumber,
        CancellationToken cancellationToken = default)
    {
        var request = new GetBuildVersionDetailsRequest
        {
            BuildNumber = buildNumber.ToString(),
            IncludeVersionControlLog = true
        };

        var response = await _httpClient
            .PostAsJsonAsync(_options.GetBuildVersionDetailsPath, request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var wrapped = await response.Content
            .ReadFromJsonAsync<RemoteApiResponse<RemoteBuildVersionDetails>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return wrapped?.Result;
    }

    public async Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .GetAsync(_options.ListEnvironmentsPath, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wrapped = await response.Content
            .ReadFromJsonAsync<RemoteApiResponse<RemoteEnvironmentDetails[]>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return wrapped?.Result ?? [];
    }
}
