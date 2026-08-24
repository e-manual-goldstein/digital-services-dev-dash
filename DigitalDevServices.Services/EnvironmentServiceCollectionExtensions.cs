using DigitalDevServices.Model.Environments;
using DigitalDevServices.Model.Tfs;
using DigitalDevServices.Services.Environments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class EnvironmentServiceCollectionExtensions
{
    public static IServiceCollection AddEnvironmentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EnvironmentCacheOptions>(configuration.GetSection(EnvironmentCacheOptions.SectionName));
        services.Configure<RemoteEnvironmentApiOptions>(configuration.GetSection(RemoteEnvironmentApiOptions.SectionName));
        services.Configure<TfsOptions>(configuration.GetSection(TfsOptions.SectionName));

        services.AddMemoryCache();

        var apiOptions = configuration.GetSection(RemoteEnvironmentApiOptions.SectionName).Get<RemoteEnvironmentApiOptions>()
            ?? new RemoteEnvironmentApiOptions();

        if (!string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
        {
            services.AddHttpClient<IRemoteEnvironmentApiClient, HttpRemoteEnvironmentApiClient>(client =>
                {
                    client.BaseAddress = new Uri(apiOptions.BaseUrl.TrimEnd('/') + "/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => RemoteEnvironmentApiHttpHandlerFactory.Create(apiOptions));
        }
        else
        {
            services.AddSingleton<IRemoteEnvironmentApiClient, UnconfiguredRemoteEnvironmentApiClient>();
        }

        services.AddScoped<IEnvironmentService, EnvironmentService>();

        return services;
    }
}
