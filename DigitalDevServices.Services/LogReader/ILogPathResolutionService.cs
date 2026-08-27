using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public interface ILogPathResolutionService
{
    Task<LogPathResolutionResult> EnsureLogPathAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default);
}
