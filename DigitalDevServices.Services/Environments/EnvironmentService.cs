using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DigitalDevServices.Services.Environments;

public sealed class EnvironmentService : IEnvironmentService
{
    private const string CacheKeyPrefix = "environment:";
    private const string CatalogCacheKey = "environment-catalog";

    private readonly DevDashDbContext _db;
    private readonly IRemoteEnvironmentApiClient _apiClient;
    private readonly IBuildVersionDetailsService _buildVersionDetailsService;
    private readonly IEnvironmentInstanceSnapshotSyncService _snapshotSyncService;
    private readonly IMemoryCache _memoryCache;
    private readonly EnvironmentCacheOptions _cacheOptions;

    public EnvironmentService(
        DevDashDbContext db,
        IRemoteEnvironmentApiClient apiClient,
        IBuildVersionDetailsService buildVersionDetailsService,
        IEnvironmentInstanceSnapshotSyncService snapshotSyncService,
        IMemoryCache memoryCache,
        IOptions<EnvironmentCacheOptions> cacheOptions)
    {
        _db = db;
        _apiClient = apiClient;
        _buildVersionDetailsService = buildVersionDetailsService;
        _snapshotSyncService = snapshotSyncService;
        _memoryCache = memoryCache;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<IReadOnlyList<CachedEnvironment>> GetEnvironmentsAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceRefresh
            && _memoryCache.TryGetValue(CatalogCacheKey, out IReadOnlyList<CachedEnvironment>? cachedCatalog)
            && cachedCatalog is not null)
        {
            return cachedCatalog
                .Select(environment => environment with { IsFromCache = true })
                .ToList();
        }

        var remoteEnvironments = await _apiClient.ListEnvironmentsAsync(cancellationToken).ConfigureAwait(false);
        var updatedAt = DateTimeOffset.UtcNow;
        var results = new List<CachedEnvironment>(remoteEnvironments.Count);

        foreach (var details in remoteEnvironments.OrderBy(environment => environment.Name, StringComparer.OrdinalIgnoreCase))
        {
            var tracked = await EnsureTrackedAsync(details.Id, updatedAt, cancellationToken).ConfigureAwait(false);
            var cachedEnvironment = BuildCatalogCachedEnvironment(tracked, details, updatedAt);
            _memoryCache.Set(GetCacheKey(tracked.Id), cachedEnvironment, _cacheOptions.CacheLifetime);
            results.Add(cachedEnvironment);
        }

        _memoryCache.Set(CatalogCacheKey, results, _cacheOptions.CacheLifetime);
        return results;
    }

    public async Task<CachedEnvironment?> GetTrackedEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default)
    {
        var tracked = await _db.TrackedEnvironments
            .AsNoTracking()
            .SingleOrDefaultAsync(environment => environment.Id == localId, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            return null;
        }

        return await GetOrFetchAsync(tracked, forceRefresh: false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CachedEnvironment> RefreshEnvironmentAsync(int remoteId, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(CatalogCacheKey);

        var environmentCode = await ResolveEnvironmentCodeAsync(remoteId, cancellationToken).ConfigureAwait(false);
        var details = await _apiClient.GetEnvironmentAsync(environmentCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Remote environment '{remoteId}' was not found via the Web API.");
        var deploymentDetails = await _apiClient
            .GetDeploymentDetailsForEnvironmentAsync(environmentCode, cancellationToken)
            .ConfigureAwait(false);

        var updatedAt = DateTimeOffset.UtcNow;
        var tracked = await EnsureTrackedAsync(details.Id, updatedAt, cancellationToken).ConfigureAwait(false);
        var instances = await LoadInstancesForEnvironmentAsync(tracked.Id, cancellationToken).ConfigureAwait(false);
        var buildVersionDetails = await LoadBuildVersionDetailsAsync(
            deploymentDetails,
            instances,
            cancellationToken).ConfigureAwait(false);

        var snapshot = EnvironmentRefreshSnapshotCollector.Create(
            details,
            deploymentDetails,
            buildVersionDetails,
            updatedAt);

        await _snapshotSyncService
            .SyncInstancesAsync(tracked.Id, snapshot, cancellationToken)
            .ConfigureAwait(false);

        var result = ToCachedEnvironment(tracked, snapshot, isFromCache: false);
        _memoryCache.Set(GetCacheKey(tracked.Id), result, _cacheOptions.CacheLifetime);
        return result;
    }

    public async Task<CachedEnvironment> SetFavouriteAsync(
        Guid localId,
        bool isFavourite,
        CancellationToken cancellationToken = default)
    {
        var tracked = await _db.TrackedEnvironments
            .SingleOrDefaultAsync(environment => environment.Id == localId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Environment '{localId}' is not tracked.");

        tracked.IsFavourite = isFavourite;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        UpdateCachedFavourite(localId, isFavourite);

        var cached = await GetTrackedEnvironmentAsync(localId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Environment '{localId}' is not tracked.");

        return cached with { IsFavourite = isFavourite };
    }

    public async Task UntrackEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default)
    {
        var tracked = await _db.TrackedEnvironments
            .SingleOrDefaultAsync(environment => environment.Id == localId, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            return;
        }

        _db.TrackedEnvironments.Remove(tracked);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _memoryCache.Remove(GetCacheKey(localId));
        _memoryCache.Remove(CatalogCacheKey);
    }

    private CachedEnvironment BuildCatalogCachedEnvironment(
        TrackedEnvironment tracked,
        RemoteEnvironmentDetails listDetails,
        DateTimeOffset updatedAt)
    {
        if (_memoryCache.TryGetValue(GetCacheKey(tracked.Id), out CachedEnvironment? existing)
            && existing?.RefreshSnapshot is { } snapshot)
        {
            return ToCachedEnvironment(tracked, snapshot, isFromCache: false) with
            {
                IsFavourite = tracked.IsFavourite
            };
        }

        return ToCachedEnvironment(
            tracked,
            listDetails,
            deploymentDetails: null,
            updatedAt,
            refreshSnapshot: null,
            isFromCache: false);
    }

    private async Task<string> ResolveEnvironmentCodeAsync(int remoteId, CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CatalogCacheKey, out IReadOnlyList<CachedEnvironment>? catalog) && catalog is not null)
        {
            var catalogMatch = catalog.FirstOrDefault(environment => environment.RemoteId == remoteId);
            if (!string.IsNullOrWhiteSpace(catalogMatch?.Details.Code))
            {
                return catalogMatch.Details.Code;
            }
        }

        var tracked = await _db.TrackedEnvironments
            .AsNoTracking()
            .SingleOrDefaultAsync(environment => environment.RemoteId == remoteId, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is not null
            && _memoryCache.TryGetValue(GetCacheKey(tracked.Id), out CachedEnvironment? cached)
            && cached is not null
            && !string.IsNullOrWhiteSpace(cached.Details.Code))
        {
            return cached.Details.Code;
        }

        var environments = await _apiClient.ListEnvironmentsAsync(cancellationToken).ConfigureAwait(false);
        var match = environments.FirstOrDefault(environment => environment.Id == remoteId);
        if (match is null || string.IsNullOrWhiteSpace(match.Code))
        {
            throw new InvalidOperationException($"Remote environment '{remoteId}' was not found via the Web API.");
        }

        return match.Code;
    }

    private async Task<TrackedEnvironment> EnsureTrackedAsync(
        int remoteId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var tracked = await _db.TrackedEnvironments
            .SingleOrDefaultAsync(environment => environment.RemoteId == remoteId, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            tracked = new TrackedEnvironment
            {
                Id = Guid.NewGuid(),
                RemoteId = remoteId,
                DateLastUpdated = updatedAt
            };
            _db.TrackedEnvironments.Add(tracked);
        }
        else
        {
            tracked.DateLastUpdated = updatedAt;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tracked;
    }

    private async Task<CachedEnvironment> GetOrFetchAsync(
        TrackedEnvironment tracked,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(tracked.Id);

        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out CachedEnvironment? cached) && cached is not null)
        {
            return cached with
            {
                IsFavourite = tracked.IsFavourite,
                DisplayOrder = tracked.DisplayOrder,
                IsFromCache = true
            };
        }

        return await RefreshEnvironmentAsync(tracked.RemoteId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ApplicationInstance>> LoadInstancesForEnvironmentAsync(
        Guid environmentLocalId,
        CancellationToken cancellationToken) =>
        await _db.ApplicationInstances
            .AsNoTracking()
            .Include(instance => instance.DeployableApplication)
            .Where(instance => instance.EnvironmentId == environmentLocalId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<IReadOnlyDictionary<string, RemoteBuildVersionDetails>> LoadBuildVersionDetailsAsync(
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        IReadOnlyList<ApplicationInstance> instances,
        CancellationToken cancellationToken)
    {
        var buildNumbers = EnvironmentRefreshSnapshotCollector.CollectPipelineBuildNumbers(
            deploymentDetails,
            instances);

        if (buildNumbers.Count == 0)
        {
            return new Dictionary<string, RemoteBuildVersionDetails>(StringComparer.OrdinalIgnoreCase);
        }

        var results = new Dictionary<string, RemoteBuildVersionDetails>(StringComparer.OrdinalIgnoreCase);

        foreach (var buildNumber in buildNumbers)
        {
            var details = await _buildVersionDetailsService
                .GetBuildVersionDetailsAsync(buildNumber, cancellationToken)
                .ConfigureAwait(false);

            if (details is not null)
            {
                results[buildNumber] = details;
            }
        }

        return results;
    }

    private static CachedEnvironment ToCachedEnvironment(
        TrackedEnvironment tracked,
        EnvironmentRefreshSnapshot snapshot,
        bool isFromCache) =>
        ToCachedEnvironment(
            tracked,
            snapshot.Details,
            snapshot.DeploymentDetails,
            snapshot.DateLastRefreshed,
            snapshot,
            isFromCache);

    private static CachedEnvironment ToCachedEnvironment(
        TrackedEnvironment tracked,
        RemoteEnvironmentDetails details,
        RemoteEnvironmentDeploymentDetails? deploymentDetails,
        DateTimeOffset updatedAt,
        EnvironmentRefreshSnapshot? refreshSnapshot,
        bool isFromCache) =>
        new()
        {
            LocalId = tracked.Id,
            RemoteId = tracked.RemoteId,
            IsFavourite = tracked.IsFavourite,
            DisplayOrder = tracked.DisplayOrder,
            Details = details,
            DeploymentDetails = deploymentDetails,
            RefreshSnapshot = refreshSnapshot,
            DateLastUpdated = updatedAt,
            IsFromCache = isFromCache
        };

    private void UpdateCachedFavourite(Guid localId, bool isFavourite)
    {
        var cacheKey = GetCacheKey(localId);
        if (_memoryCache.TryGetValue(cacheKey, out CachedEnvironment? cached) && cached is not null)
        {
            _memoryCache.Set(cacheKey, cached with { IsFavourite = isFavourite }, _cacheOptions.CacheLifetime);
        }

        if (_memoryCache.TryGetValue(CatalogCacheKey, out IReadOnlyList<CachedEnvironment>? catalog) && catalog is not null)
        {
            var updatedCatalog = catalog
                .Select(environment => environment.LocalId == localId
                    ? environment with { IsFavourite = isFavourite }
                    : environment)
                .ToList();

            _memoryCache.Set(CatalogCacheKey, updatedCatalog, _cacheOptions.CacheLifetime);
        }
    }

    private static string GetCacheKey(Guid localId) => CacheKeyPrefix + localId.ToString("N");
}
