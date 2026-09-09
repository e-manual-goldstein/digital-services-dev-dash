using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Services.Configuration;

public interface IPinnedConfigurationKeyService
{
    Task<IReadOnlyList<PinnedConfigurationKey>> GetAllAsync(CancellationToken cancellationToken = default);

    Task PinAsync(string key, CancellationToken cancellationToken = default);

    Task UnpinAsync(string key, CancellationToken cancellationToken = default);
}
