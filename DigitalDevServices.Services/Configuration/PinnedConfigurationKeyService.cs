using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.Configuration;

public sealed class PinnedConfigurationKeyService : IPinnedConfigurationKeyService
{
    private readonly DevDashDbContext _db;

    public PinnedConfigurationKeyService(DevDashDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PinnedConfigurationKey>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.PinnedConfigurationKeys
            .AsNoTracking()
            .OrderBy(pinnedKey => pinnedKey.DisplayOrder)
            .ThenBy(pinnedKey => pinnedKey.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task PinAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeRequiredText(key, nameof(key));
        var existing = await FindByKeyAsync(normalizedKey, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var maxDisplayOrder = await _db.PinnedConfigurationKeys
            .Select(pinnedKey => (int?)pinnedKey.DisplayOrder)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) ?? -1;

        _db.PinnedConfigurationKeys.Add(new PinnedConfigurationKey
        {
            Id = Guid.NewGuid(),
            Key = normalizedKey,
            DisplayOrder = maxDisplayOrder + 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnpinAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeRequiredText(key, nameof(key));
        var existing = await FindByKeyAsync(normalizedKey, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        _db.PinnedConfigurationKeys.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PinnedConfigurationKey?> FindByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var pinnedKeys = await _db.PinnedConfigurationKeys
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return pinnedKeys.SingleOrDefault(pinnedKey =>
            string.Equals(pinnedKey.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRequiredText(string value, string paramName)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return trimmed;
    }
}
