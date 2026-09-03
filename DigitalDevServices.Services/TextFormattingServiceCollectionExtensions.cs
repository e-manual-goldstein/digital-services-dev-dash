using DigitalDevServices.Services.TextFormatting;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class TextFormattingServiceCollectionExtensions
{
    public static IServiceCollection AddTextFormattingServices(this IServiceCollection services)
    {
        services.AddSingleton<IFormattedTextService, FormattedTextService>();
        return services;
    }
}
