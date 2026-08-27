using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public interface ILogParsingService
{
    Task<IReadOnlyList<ParsedLogEntry>> ParseForDeployableApplicationAsync(
        Guid deployableApplicationId,
        string content,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ParsedLogEntry> ParseWithFormat(string formatName, string content, string? parserConfig = null);

    IReadOnlyList<string> GetSupportedFormatNames();
}
