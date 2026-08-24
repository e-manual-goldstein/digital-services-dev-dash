using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed class LogParsingService : ILogParsingService
{
    private readonly ILogFormatProfileService _profileService;
    private readonly LogParserRegistry _parserRegistry;

    public LogParsingService(ILogFormatProfileService profileService, LogParserRegistry parserRegistry)
    {
        _profileService = profileService;
        _parserRegistry = parserRegistry;
    }

    public async Task<IReadOnlyList<ParsedLogEntry>> ParseForDeployableApplicationAsync(
        Guid deployableApplicationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService
            .GetByDeployableApplicationIdAsync(deployableApplicationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No log format profile is configured for deployable application '{deployableApplicationId}'.");

        return ParseWithFormat(profile.FormatName, content);
    }

    public IReadOnlyList<ParsedLogEntry> ParseWithFormat(string formatName, string content)
    {
        var parser = _parserRegistry.GetRequiredParser(formatName);
        return parser.Parse(content);
    }

    public IReadOnlyList<string> GetSupportedFormatNames()
    {
        return LogFormatNames.All;
    }
}
