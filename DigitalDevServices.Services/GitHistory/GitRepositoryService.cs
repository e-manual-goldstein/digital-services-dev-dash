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
        return await QueryWithIncludes()
            .OrderBy(repository => repository.Name.ToLower())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GitRepository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await QueryWithIncludes()
            .SingleOrDefaultAsync(repository => repository.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GitRepository> CreateAsync(
        GitRepositoryUpsert upsert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsert);

        var normalizedName = NormalizeRequiredText(upsert.Name, nameof(upsert.Name));
        var currentLocationUrl = NormalizeRequiredUrl(upsert.CurrentLocationUrl, nameof(upsert.CurrentLocationUrl));

        await EnsureNameIsUniqueAsync(normalizedName, excludeId: null, cancellationToken).ConfigureAwait(false);

        var repository = new GitRepository
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            DateMigrated = upsert.DateMigrated,
            CurrentLocationUrl = currentLocationUrl,
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
        var currentLocationUrl = NormalizeRequiredUrl(upsert.CurrentLocationUrl, nameof(upsert.CurrentLocationUrl));

        await EnsureNameIsUniqueAsync(normalizedName, excludeId: id, cancellationToken).ConfigureAwait(false);

        repository.Name = normalizedName;
        repository.DateMigrated = upsert.DateMigrated;
        repository.CurrentLocationUrl = currentLocationUrl;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await GetByIdAsync(id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var repository = await _db.GitRepositories
            .Include(item => item.PreviousLocations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (repository is null)
        {
            return;
        }

        _db.GitRepositories.Remove(repository);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<HistoricGitRepoRecord> AddHistoricRecordAsync(
        Guid repositoryId,
        HistoricGitRepoRecordUpsert upsert,
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

        var record = new HistoricGitRepoRecord
        {
            Id = Guid.NewGuid(),
            GitRepositoryId = repositoryId,
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

    private IQueryable<GitRepository> QueryWithIncludes()
    {
        return _db.GitRepositories
            .AsNoTracking()
            .Include(repository => repository.PreviousLocations);
    }

    private async Task EnsureNameIsUniqueAsync(
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
