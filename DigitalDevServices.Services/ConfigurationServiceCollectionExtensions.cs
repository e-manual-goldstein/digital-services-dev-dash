using DigitalDevServices.Services.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddScoped<IConfigurationSettingService, ConfigurationSettingService>();

        return services;
    }
}
