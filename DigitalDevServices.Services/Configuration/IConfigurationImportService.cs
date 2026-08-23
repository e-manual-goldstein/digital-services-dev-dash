using DigitalDevServices.Model.Configuration;

namespace DigitalDevServices.Services.Configuration;

public interface IConfigurationImportService
{
    Task<ConfigurationImportResult> RefreshAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default);
}
