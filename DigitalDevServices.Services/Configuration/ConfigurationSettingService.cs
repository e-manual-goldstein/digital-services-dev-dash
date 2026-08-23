using DigitalDevServices.Data;
using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.Configuration;

public sealed class ConfigurationSettingService : IConfigurationSettingService
{
    private readonly DevDashDbContext _db;

    public ConfigurationSettingService(DevDashDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ConfigurationSetting>> GetByApplicationInstanceIdAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ConfigurationSettings
            .AsNoTracking()
            .Where(setting => setting.ApplicationInstanceId == applicationInstanceId)
            .OrderBy(setting => setting.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ConfigurationSetting?> GetByApplicationInstanceAndKeyAsync(
        Guid applicationInstanceId,
        string key,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeRequiredText(key, nameof(key));

        return await _db.ConfigurationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                setting => setting.ApplicationInstanceId == applicationInstanceId && setting.Key == normalizedKey,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ConfigurationSetting> UpsertAsync(
        ConfigurationSettingUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var key = NormalizeRequiredText(upsert.Key, nameof(upsert.Key));
        var value = upsert.Value ?? string.Empty;
        var source = NormalizeOptionalText(upsert.Source);

        await EnsureApplicationInstanceExistsAsync(upsert.ApplicationInstanceId, cancellationToken)
            .ConfigureAwait(false);

        var existing = await _db.ConfigurationSettings
            .SingleOrDefaultAsync(
                setting => setting.ApplicationInstanceId == upsert.ApplicationInstanceId && setting.Key == key,
                cancellationToken)
            .ConfigureAwait(false);

        var capturedAt = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            existing = new ConfigurationSetting
            {
                Id = Guid.NewGuid(),
                ApplicationInstanceId = upsert.ApplicationInstanceId,
                Key = key
            };
            _db.ConfigurationSettings.Add(existing);
        }

        existing.Value = value;
        existing.Source = source;
        existing.CapturedAt = capturedAt;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetByApplicationInstanceAndKeyAsync(upsert.ApplicationInstanceId, key, cancellationToken)
            .ConfigureAwait(false))!;
    }

    private async Task EnsureApplicationInstanceExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _db.ApplicationInstances.AnyAsync(instance => instance.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            throw new InvalidOperationException($"Application instance '{id}' was not found.");
        }
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

    private static string? NormalizeOptionalText(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
