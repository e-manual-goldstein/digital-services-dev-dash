using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Logs;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.Logs;

public sealed class LogFormatProfileService : ILogFormatProfileService
{
    private readonly DevDashDbContext _db;
    private readonly LogParserRegistry _parserRegistry;

    public LogFormatProfileService(DevDashDbContext db, LogParserRegistry parserRegistry)
    {
        _db = db;
        _parserRegistry = parserRegistry;
    }

    public async Task<LogFormatProfile?> GetByDeployableApplicationIdAsync(
        Guid deployableApplicationId,
        CancellationToken cancellationToken = default)
    {
        return await _db.LogFormatProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.DeployableApplicationId == deployableApplicationId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LogFormatProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.LogFormatProfiles
            .AsNoTracking()
            .OrderBy(profile => profile.FormatName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<LogFormatProfile> UpsertAsync(
        LogFormatProfileUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var formatName = NormalizeRequiredText(upsert.FormatName, nameof(upsert.FormatName));
        if (!_parserRegistry.TryGetParser(formatName, out _))
        {
            throw new InvalidOperationException($"Log format '{formatName}' is not supported.");
        }

        await EnsureDeployableApplicationExistsAsync(upsert.DeployableApplicationId, cancellationToken)
            .ConfigureAwait(false);

        var existing = await _db.LogFormatProfiles
            .SingleOrDefaultAsync(profile => profile.DeployableApplicationId == upsert.DeployableApplicationId, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            existing = new LogFormatProfile
            {
                Id = Guid.NewGuid(),
                DeployableApplicationId = upsert.DeployableApplicationId
            };
            _db.LogFormatProfiles.Add(existing);
        }

        existing.FormatName = formatName;
        existing.ParserConfig = NormalizeParserConfig(upsert.ParserConfig);
        existing.Notes = NormalizeOptionalText(upsert.Notes);
        existing.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetByDeployableApplicationIdAsync(upsert.DeployableApplicationId, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task DeleteByDeployableApplicationIdAsync(
        Guid deployableApplicationId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.LogFormatProfiles
            .SingleOrDefaultAsync(profile => profile.DeployableApplicationId == deployableApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        _db.LogFormatProfiles.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureDeployableApplicationExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _db.DeployableApplications.AnyAsync(app => app.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            throw new InvalidOperationException($"Deployable application '{id}' was not found.");
        }
    }

    private static string NormalizeParserConfig(string? parserConfig)
    {
        var trimmed = parserConfig?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "{}" : trimmed;
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
