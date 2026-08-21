using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Services.PipelineFeeds;

public interface IPipelineFeedService
{
    Task<IReadOnlyList<PipelineFeed>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PipelineFeed?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PipelineFeed?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<PipelineFeed> CreateAsync(string name, string? description = null, CancellationToken cancellationToken = default);

    Task<PipelineFeed> UpdateAsync(Guid id, string name, string? description = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
