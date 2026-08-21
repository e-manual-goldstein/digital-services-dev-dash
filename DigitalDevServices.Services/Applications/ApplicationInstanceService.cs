using DigitalDevServices.Data;
using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.Applications;

public sealed class ApplicationInstanceService : IApplicationInstanceService
{
    private readonly DevDashDbContext _db;

    public ApplicationInstanceService(DevDashDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ApplicationInstance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await QueryWithIncludes()
            .OrderBy(instance => instance.DeployableApplication.Name)
            .ThenBy(instance => instance.EnvironmentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApplicationInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await QueryWithIncludes()
            .SingleOrDefaultAsync(instance => instance.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ApplicationInstance>> GetByEnvironmentIdAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithIncludes()
            .Where(instance => instance.EnvironmentId == environmentId)
            .OrderBy(instance => instance.DeployableApplication.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ApplicationInstance>> GetByDeployableApplicationIdAsync(
        Guid deployableApplicationId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithIncludes()
            .Where(instance => instance.DeployableApplicationId == deployableApplicationId)
            .OrderBy(instance => instance.EnvironmentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApplicationInstance> UpsertAsync(
        ApplicationInstanceUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var buildNumber = NormalizeRequiredText(upsert.BuildNumber, nameof(upsert.BuildNumber));

        await EnsureDeployableApplicationExistsAsync(upsert.DeployableApplicationId, cancellationToken)
            .ConfigureAwait(false);
        await EnsureEnvironmentExistsAsync(upsert.EnvironmentId, cancellationToken).ConfigureAwait(false);
        await EnsurePipelineFeedExistsAsync(upsert.PipelineFeedId, cancellationToken).ConfigureAwait(false);

        var existing = await _db.ApplicationInstances
            .SingleOrDefaultAsync(
                instance => instance.DeployableApplicationId == upsert.DeployableApplicationId
                    && instance.EnvironmentId == upsert.EnvironmentId,
                cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            existing = new ApplicationInstance
            {
                Id = Guid.NewGuid(),
                DeployableApplicationId = upsert.DeployableApplicationId,
                EnvironmentId = upsert.EnvironmentId,
                CreatedAt = now
            };
            _db.ApplicationInstances.Add(existing);
        }

        existing.BuildNumber = buildNumber;
        existing.PipelineFeedId = upsert.PipelineFeedId;
        existing.SourceBranch = NormalizeOptionalText(upsert.SourceBranch);
        existing.DeployedAt = upsert.DeployedAt;
        existing.PhysicalPath = NormalizeOptionalText(upsert.PhysicalPath);
        existing.LogPath = NormalizeOptionalText(upsert.LogPath);
        existing.SqlServerInstance = NormalizeOptionalText(upsert.SqlServerInstance);
        existing.Notes = NormalizeOptionalText(upsert.Notes);
        existing.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetByIdAsync(existing.Id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instance = await _db.ApplicationInstances
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            return;
        }

        _db.ApplicationInstances.Remove(instance);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<ApplicationInstance> QueryWithIncludes()
    {
        return _db.ApplicationInstances
            .AsNoTracking()
            .Include(instance => instance.DeployableApplication)
            .Include(instance => instance.Environment)
            .Include(instance => instance.PipelineFeed);
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

    private async Task EnsureEnvironmentExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _db.TrackedEnvironments.AnyAsync(env => env.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            throw new InvalidOperationException($"Tracked environment '{id}' was not found.");
        }
    }

    private async Task EnsurePipelineFeedExistsAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return;
        }

        var exists = await _db.PipelineFeeds.AnyAsync(feed => feed.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            throw new InvalidOperationException($"Pipeline feed '{id}' was not found.");
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
