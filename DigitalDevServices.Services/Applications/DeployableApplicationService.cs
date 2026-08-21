using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.Applications;

public sealed class DeployableApplicationService : IDeployableApplicationService
{
    private readonly DevDashDbContext _db;

    public DeployableApplicationService(DevDashDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DeployableApplication>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DeployableApplications
            .AsNoTracking()
            .OrderBy(app => app.Name, StringComparer.OrdinalIgnoreCase)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DeployableApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.DeployableApplications
            .AsNoTracking()
            .SingleOrDefaultAsync(app => app.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DeployableApplication?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName is null)
        {
            return null;
        }

        return await _db.DeployableApplications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                app => app.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DeployableApplication> CreateAsync(
        string name,
        string? projectKey = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name)
            ?? throw new ArgumentException("Deployable application name is required.", nameof(name));

        await EnsureNameIsUniqueAsync(normalizedName, excludeId: null, cancellationToken).ConfigureAwait(false);

        var application = new DeployableApplication
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            ProjectKey = NormalizeOptionalText(projectKey),
            Notes = NormalizeOptionalText(notes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.DeployableApplications.Add(application);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return application;
    }

    public async Task<DeployableApplication> UpdateAsync(
        Guid id,
        string name,
        string? projectKey = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name)
            ?? throw new ArgumentException("Deployable application name is required.", nameof(name));

        var application = await _db.DeployableApplications
            .SingleOrDefaultAsync(app => app.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Deployable application '{id}' was not found.");

        await EnsureNameIsUniqueAsync(normalizedName, excludeId: id, cancellationToken).ConfigureAwait(false);

        application.Name = normalizedName;
        application.ProjectKey = NormalizeOptionalText(projectKey);
        application.Notes = NormalizeOptionalText(notes);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return application;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _db.DeployableApplications
            .SingleOrDefaultAsync(app => app.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return;
        }

        _db.DeployableApplications.Remove(application);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureNameIsUniqueAsync(
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicateExists = await _db.DeployableApplications
            .AnyAsync(
                app => app.Name.ToLower() == normalizedName.ToLower()
                    && (excludeId == null || app.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A deployable application named '{normalizedName}' already exists.");
        }
    }

    private static string? NormalizeName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.Length == 0 ? null : trimmed;
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
