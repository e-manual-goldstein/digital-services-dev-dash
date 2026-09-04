using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.GitHistory;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.GitHistory;

public interface IGitRepositoryService
{
    Task<IReadOnlyList<GitRepository>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GitRepository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GitRepository> CreateAsync(GitRepositoryUpsert upsert, CancellationToken cancellationToken = default);

    Task<GitRepository> UpdateAsync(Guid id, GitRepositoryUpsert upsert, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ArtifactComponent?> GetComponentByIdAsync(Guid componentId, CancellationToken cancellationToken = default);

    Task<ArtifactComponent> CreateComponentAsync(
        Guid repositoryId,
        ArtifactComponentUpsert upsert,
        CancellationToken cancellationToken = default);

    Task<ArtifactComponent> UpdateComponentAsync(
        Guid componentId,
        ArtifactComponentUpsert upsert,
        CancellationToken cancellationToken = default);

    Task DeleteComponentAsync(Guid componentId, CancellationToken cancellationToken = default);

    Task<HistoricGitRepoRecord> AddHistoricRecordAsync(
        Guid componentId,
        HistoricGitRepoRecordUpsert upsert,
        CancellationToken cancellationToken = default);

    Task<HistoricGitRepoRecord> UpdateHistoricRecordAsync(
        Guid recordId,
        HistoricGitRepoRecordUpsert upsert,
        CancellationToken cancellationToken = default);

    Task DeleteHistoricRecordAsync(Guid recordId, CancellationToken cancellationToken = default);
}
