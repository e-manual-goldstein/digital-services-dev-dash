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

    public async Task<IReadOnlyList<CachedEnvironment>> GetTrackedEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        var tracked = await _db.TrackedEnvironments
            .AsNoTracking()
            .OrderBy(e => e.RemoteId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = new List<CachedEnvironment>(tracked.Count);
        foreach (var item in tracked)
        {
            results.Add(await GetOrFetchAsync(item, forceRefresh: false, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    public async Task<CachedEnvironment?> GetTrackedEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default)
    {
        var tracked = await _db.TrackedEnvironments
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == localId, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            return null;
        }

        return await GetOrFetchAsync(tracked, forceRefresh: false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CachedEnvironment> TrackEnvironmentAsync(int remoteId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.TrackedEnvironments
            .SingleOrDefaultAsync(e => e.RemoteId == remoteId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return await GetOrFetchAsync(existing, forceRefresh: true, cancellationToken).ConfigureAwait(false);
        }

        var tracked = new TrackedEnvironment
        {
            Id = Guid.NewGuid(),
            RemoteId = remoteId,
            DateLastUpdated = DateTimeOffset.MinValue
        };

        _db.TrackedEnvironments.Add(tracked);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await GetOrFetchAsync(tracked, forceRefresh: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CachedEnvironment> RefreshEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default)
    {
        var tracked = await _db.TrackedEnvironments
            .SingleOrDefaultAsync(e => e.Id == localId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tracked environment '{localId}' was not found.");

        return await GetOrFetchAsync(tracked, forceRefresh: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task UntrackEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default)
    {
        var tracked = await _db.TrackedEnvironments
            .SingleOrDefaultAsync(e => e.Id == localId, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            return;
        }

        _db.TrackedEnvironments.Remove(tracked);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _memoryCache.Remove(GetCacheKey(localId));
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

        var details = await _apiClient.GetEnvironmentAsync(tracked.RemoteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Remote environment '{tracked.RemoteId}' was not found via the Web API.");

        var updatedAt = DateTimeOffset.UtcNow;

        var entity = await _db.TrackedEnvironments
            .SingleAsync(e => e.Id == tracked.Id, cancellationToken)
            .ConfigureAwait(false);

        entity.DateLastUpdated = updatedAt;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new CachedEnvironment
        {
            LocalId = tracked.Id,
            RemoteId = tracked.RemoteId,
            Details = details,
            DateLastUpdated = updatedAt,
            IsFromCache = false
        };

        _memoryCache.Set(cacheKey, result, _cacheOptions.CacheLifetime);
        return result;
    }

    private static string GetCacheKey(Guid localId) => CacheKeyPrefix + localId.ToString("N");
}
