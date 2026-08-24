using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public interface ILogFormatProfileService
{
    Task<LogFormatProfile?> GetByDeployableApplicationIdAsync(
        Guid deployableApplicationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogFormatProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<LogFormatProfile> UpsertAsync(
        LogFormatProfileUpsert upsert,
        CancellationToken cancellationToken = default);

    Task DeleteByDeployableApplicationIdAsync(
        Guid deployableApplicationId,
        CancellationToken cancellationToken = default);
}
