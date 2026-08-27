using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public sealed class LogParsingService : ILogParsingService
{
    private readonly ILogFormatProfileService _profileService;
    private readonly LogParserRegistry _parserRegistry;
    private readonly CustomRegexLogParser _customRegexLogParser;

    public LogParsingService(
        ILogFormatProfileService profileService,
        LogParserRegistry parserRegistry,
        CustomRegexLogParser customRegexLogParser)
    {
        _profileService = profileService;
        _parserRegistry = parserRegistry;
        _customRegexLogParser = customRegexLogParser;
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

        return ParseWithFormat(profile.FormatName, content, profile.ParserConfig);
    }

    public IReadOnlyList<ParsedLogEntry> ParseWithFormat(
        string formatName,
        string content,
        string? parserConfig = null)
    {
        if (string.Equals(formatName, LogFormatNames.CustomRegex, StringComparison.OrdinalIgnoreCase))
        {
            var config = CustomRegexParserConfig.Parse(parserConfig);
            return _customRegexLogParser.Parse(content, config);
        }

        var parser = _parserRegistry.GetRequiredParser(formatName);
        return parser.Parse(content);
    }

    public IReadOnlyList<string> GetSupportedFormatNames()
    {
        return LogFormatNames.All;
    }
}
