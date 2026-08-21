using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.PipelineFeeds;

public sealed class PipelineFeedService : IPipelineFeedService
{
    private readonly DevDashDbContext _db;

    public PipelineFeedService(DevDashDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PipelineFeed>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PipelineFeeds
            .AsNoTracking()
            .OrderBy(feed => feed.Name, StringComparer.OrdinalIgnoreCase)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PipelineFeed?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.PipelineFeeds
            .AsNoTracking()
            .SingleOrDefaultAsync(feed => feed.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PipelineFeed?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName is null)
        {
            return null;
        }

        return await _db.PipelineFeeds
            .AsNoTracking()
            .SingleOrDefaultAsync(
                feed => feed.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PipelineFeed> CreateAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name)
            ?? throw new ArgumentException("Pipeline feed name is required.", nameof(name));

        await EnsureNameIsUniqueAsync(normalizedName, excludeId: null, cancellationToken).ConfigureAwait(false);

        var feed = new PipelineFeed
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Description = NormalizeDescription(description),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.PipelineFeeds.Add(feed);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return feed;
    }

    public async Task<PipelineFeed> UpdateAsync(
        Guid id,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name)
            ?? throw new ArgumentException("Pipeline feed name is required.", nameof(name));

        var feed = await _db.PipelineFeeds
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pipeline feed '{id}' was not found.");

        await EnsureNameIsUniqueAsync(normalizedName, excludeId: id, cancellationToken).ConfigureAwait(false);

        feed.Name = normalizedName;
        feed.Description = NormalizeDescription(description);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return feed;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var feed = await _db.PipelineFeeds
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (feed is null)
        {
            return;
        }

        _db.PipelineFeeds.Remove(feed);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureNameIsUniqueAsync(
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicateExists = await _db.PipelineFeeds
            .AnyAsync(
                feed => feed.Name.ToLower() == normalizedName.ToLower()
                    && (excludeId == null || feed.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A pipeline feed named '{normalizedName}' already exists.");
        }
    }

    private static string? NormalizeName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (description is null)
        {
            return null;
        }

        var trimmed = description.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
