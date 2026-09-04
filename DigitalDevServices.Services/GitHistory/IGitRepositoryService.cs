using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.GitHistory;

namespace DigitalDevServices.Services.GitHistory;

public interface IGitRepositoryService
{
    Task<IReadOnlyList<GitRepository>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GitRepository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GitRepository> CreateAsync(GitRepositoryUpsert upsert, CancellationToken cancellationToken = default);

    Task<GitRepository> UpdateAsync(Guid id, GitRepositoryUpsert upsert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HistoricGitRepoRecord> AddHistoricRecordAsync(
        Guid repositoryId,
        HistoricGitRepoRecordUpsert upsert,
        CancellationToken cancellationToken = default);

    Task<HistoricGitRepoRecord> UpdateHistoricRecordAsync(
        Guid recordId,
        HistoricGitRepoRecordUpsert upsert,
        CancellationToken cancellationToken = default);

    Task DeleteHistoricRecordAsync(Guid recordId, CancellationToken cancellationToken = default);
}
