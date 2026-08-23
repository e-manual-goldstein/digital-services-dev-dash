using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Services.Configuration;

public interface IConfigurationSettingService
{
    Task<IReadOnlyList<ConfigurationSetting>> GetByApplicationInstanceIdAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default);

    Task<ConfigurationSetting?> GetByApplicationInstanceAndKeyAsync(
        Guid applicationInstanceId,
        string key,
        CancellationToken cancellationToken = default);

    Task<ConfigurationSetting> UpsertAsync(
        ConfigurationSettingUpsert upsert,
        CancellationToken cancellationToken = default);
}
