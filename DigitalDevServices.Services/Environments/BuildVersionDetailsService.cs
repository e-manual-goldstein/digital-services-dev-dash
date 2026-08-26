using DigitalDevServices.Model.Environments;
using Microsoft.Extensions.Caching.Memory;

namespace DigitalDevServices.Services.Environments;

public sealed class BuildVersionDetailsService : IBuildVersionDetailsService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(15);

    private readonly IRemoteEnvironmentApiClient _apiClient;
    private readonly IMemoryCache _memoryCache;

    public BuildVersionDetailsService(IRemoteEnvironmentApiClient apiClient, IMemoryCache memoryCache)
    {
        _apiClient = apiClient;
        _memoryCache = memoryCache;
    }

    public async Task<RemoteBuildVersionDetails?> GetBuildVersionDetailsAsync(
        string environmentPipelineBuildNumber,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(environmentPipelineBuildNumber);
        if (_memoryCache.TryGetValue(cacheKey, out RemoteBuildVersionDetails? cached))
        {
            return cached;
        }

        var details = await _apiClient
            .GetBuildVersionDetailsAsync(environmentPipelineBuildNumber, cancellationToken)
            .ConfigureAwait(false);

        if (details is not null)
        {
            _memoryCache.Set(cacheKey, details, CacheLifetime);
        }

        return details;
    }

    private static string GetCacheKey(string buildNumber) => $"build-version:{buildNumber}";
}
