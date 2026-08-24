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
    private readonly IMemoryCache _memoryCache;
    private readonly EnvironmentCacheOptions _cacheOptions;

    public EnvironmentService(
        DevDashDbContext db,
        IRemoteEnvironmentApiClient apiClient,
        IMemoryCache memoryCache,
        IOptions<EnvironmentCacheOptions> cacheOptions)
    {
        _db = db;
        _apiClient = apiClient;
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
            var cachedEnvironment = ToCachedEnvironment(tracked, details, updatedAt, isFromCache: false);
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

        var updatedAt = DateTimeOffset.UtcNow;
        var tracked = await EnsureTrackedAsync(details.Id, updatedAt, cancellationToken).ConfigureAwait(false);
        var result = ToCachedEnvironment(tracked, details, updatedAt, isFromCache: false);
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
            return cached with { IsFromCache = true };
        }

        return await RefreshEnvironmentAsync(tracked.RemoteId, cancellationToken).ConfigureAwait(false);
    }

    private static CachedEnvironment ToCachedEnvironment(
        TrackedEnvironment tracked,
        RemoteEnvironmentDetails details,
        DateTimeOffset updatedAt,
        bool isFromCache)
    {
        return new CachedEnvironment
        {
            LocalId = tracked.Id,
            RemoteId = tracked.RemoteId,
            IsFavourite = tracked.IsFavourite,
            Details = details,
            DateLastUpdated = updatedAt,
            IsFromCache = isFromCache
        };
    }

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
