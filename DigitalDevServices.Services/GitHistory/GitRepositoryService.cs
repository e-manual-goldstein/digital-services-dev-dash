using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.GitHistory;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.GitHistory;

public sealed class GitRepositoryService : IGitRepositoryService
{
    private readonly DevDashDbContext _db;

    public GitRepositoryService(DevDashDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GitRepository>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await QueryRepositoriesWithIncludes()
            .OrderBy(repository => repository.Name.ToLower())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GitRepository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await QueryRepositoriesWithIncludes()
            .SingleOrDefaultAsync(repository => repository.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GitRepository> CreateAsync(
        GitRepositoryUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var normalizedName = NormalizeRequiredText(upsert.Name, nameof(upsert.Name));
        await EnsureRepositoryNameIsUniqueAsync(normalizedName, excludeId: null, cancellationToken)
            .ConfigureAwait(false);

        var repository = new GitRepository
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.GitRepositories.Add(repository);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetByIdAsync(repository.Id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<GitRepository> UpdateAsync(
        Guid id,
        GitRepositoryUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var repository = await _db.GitRepositories
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Git repository '{id}' was not found.");

        var normalizedName = NormalizeRequiredText(upsert.Name, nameof(upsert.Name));
        await EnsureRepositoryNameIsUniqueAsync(normalizedName, excludeId: id, cancellationToken)
            .ConfigureAwait(false);

        repository.Name = normalizedName;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetByIdAsync(id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var repository = await _db.GitRepositories
            .Include(item => item.ArtifactComponents)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (repository is null)
        {
            return;
        }

        _db.GitRepositories.Remove(repository);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ArtifactComponent?> GetComponentByIdAsync(
        Guid componentId,
        CancellationToken cancellationToken = default)
    {
        return await QueryComponentsWithIncludes()
            .SingleOrDefaultAsync(component => component.Id == componentId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ArtifactComponent> CreateComponentAsync(
        Guid repositoryId,
        ArtifactComponentUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var repositoryExists = await _db.GitRepositories
            .AnyAsync(repository => repository.Id == repositoryId, cancellationToken)
            .ConfigureAwait(false);

        if (!repositoryExists)
        {
            throw new InvalidOperationException($"Git repository '{repositoryId}' was not found.");
        }

        var normalizedName = NormalizeRequiredText(upsert.Name, nameof(upsert.Name));
        var currentLocationUrl = NormalizeRequiredUrl(upsert.CurrentLocationUrl, nameof(upsert.CurrentLocationUrl));

        await EnsureComponentNameIsUniqueAsync(repositoryId, normalizedName, excludeId: null, cancellationToken)
            .ConfigureAwait(false);

        var component = new ArtifactComponent
        {
            Id = Guid.NewGuid(),
            GitRepositoryId = repositoryId,
            Name = normalizedName,
            DateMigrated = upsert.DateMigrated,
            CurrentLocationUrl = currentLocationUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ArtifactComponents.Add(component);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetComponentByIdAsync(component.Id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<ArtifactComponent> UpdateComponentAsync(
        Guid componentId,
        ArtifactComponentUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var component = await _db.ArtifactComponents
            .SingleOrDefaultAsync(item => item.Id == componentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Artifact component '{componentId}' was not found.");

        var normalizedName = NormalizeRequiredText(upsert.Name, nameof(upsert.Name));
        var currentLocationUrl = NormalizeRequiredUrl(upsert.CurrentLocationUrl, nameof(upsert.CurrentLocationUrl));

        await EnsureComponentNameIsUniqueAsync(
                component.GitRepositoryId,
                normalizedName,
                excludeId: componentId,
                cancellationToken)
            .ConfigureAwait(false);

        component.Name = normalizedName;
        component.DateMigrated = upsert.DateMigrated;
        component.CurrentLocationUrl = currentLocationUrl;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetComponentByIdAsync(componentId, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task DeleteComponentAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        var component = await _db.ArtifactComponents
            .Include(item => item.PreviousLocations)
            .SingleOrDefaultAsync(item => item.Id == componentId, cancellationToken)
            .ConfigureAwait(false);

        if (component is null)
        {
            return;
        }

        _db.ArtifactComponents.Remove(component);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<HistoricGitRepoRecord> AddHistoricRecordAsync(
        Guid componentId,
        HistoricGitRepoRecordUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var componentExists = await _db.ArtifactComponents
            .AnyAsync(component => component.Id == componentId, cancellationToken)
            .ConfigureAwait(false);

        if (!componentExists)
        {
            throw new InvalidOperationException($"Artifact component '{componentId}' was not found.");
        }

        var record = new HistoricGitRepoRecord
        {
            Id = Guid.NewGuid(),
            ArtifactComponentId = componentId,
            Name = NormalizeRequiredText(upsert.Name, nameof(upsert.Name)),
            LastLocationUrl = NormalizeRequiredUrl(upsert.LastLocationUrl, nameof(upsert.LastLocationUrl)),
            DateMigrated = upsert.DateMigrated
        };

        _db.HistoricGitRepoRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return record;
    }

    public async Task<HistoricGitRepoRecord> UpdateHistoricRecordAsync(
        Guid recordId,
        HistoricGitRepoRecordUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var record = await _db.HistoricGitRepoRecords
            .SingleOrDefaultAsync(item => item.Id == recordId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Historic git repository record '{recordId}' was not found.");

        record.Name = NormalizeRequiredText(upsert.Name, nameof(upsert.Name));
        record.LastLocationUrl = NormalizeRequiredUrl(upsert.LastLocationUrl, nameof(upsert.LastLocationUrl));
        record.DateMigrated = upsert.DateMigrated;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return record;
    }

    public async Task DeleteHistoricRecordAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var record = await _db.HistoricGitRepoRecords
            .SingleOrDefaultAsync(item => item.Id == recordId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return;
        }

        _db.HistoricGitRepoRecords.Remove(record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<GitRepository> QueryRepositoriesWithIncludes()
    {
        return _db.GitRepositories
            .AsNoTracking()
            .Include(repository => repository.ArtifactComponents)
                .ThenInclude(component => component.PreviousLocations);
    }

    private IQueryable<ArtifactComponent> QueryComponentsWithIncludes()
    {
        return _db.ArtifactComponents
            .AsNoTracking()
            .Include(component => component.GitRepository)
            .Include(component => component.PreviousLocations);
    }

    private async Task EnsureRepositoryNameIsUniqueAsync(
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicateExists = await _db.GitRepositories
            .AnyAsync(
                repository => repository.Name.ToLower() == normalizedName.ToLower()
                    && (excludeId == null || repository.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A git repository named '{normalizedName}' already exists.");
        }
    }

    private async Task EnsureComponentNameIsUniqueAsync(
        Guid repositoryId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicateExists = await _db.ArtifactComponents
            .AnyAsync(
                component => component.GitRepositoryId == repositoryId
                    && component.Name.ToLower() == normalizedName.ToLower()
                    && (excludeId == null || component.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                $"A component named '{normalizedName}' already exists in this repository.");
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

    private static string NormalizeRequiredUrl(string value, string paramName)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("A valid http or https URL is required.", paramName);
        }

        return trimmed;
    }
}
