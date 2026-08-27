using DigitalDevServices.Services.Logs;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class LogServiceCollectionExtensions
{
    public static IServiceCollection AddLogServices(this IServiceCollection services)
    {
        services.AddSingleton<ILogEntryParser, SerilogJsonLogParser>();
        services.AddSingleton<ILogEntryParser, PlainTextLogParser>();
        services.AddSingleton<ILogEntryParser, NLogMultilineLogParser>();
        services.AddSingleton<ILogEntryParser, Log4NetPatternLogParser>();
        services.AddSingleton<LogParserRegistry>();
        services.AddSingleton<CustomRegexLogParser>();
        services.AddScoped<ILogFormatProfileService, LogFormatProfileService>();
        services.AddScoped<ILogParsingService, LogParsingService>();
        services.AddScoped<ILogReaderService, LogReaderService>();
        services.AddScoped<ILogPathResolutionService, LogPathResolutionService>();

        return services;
    }
}
