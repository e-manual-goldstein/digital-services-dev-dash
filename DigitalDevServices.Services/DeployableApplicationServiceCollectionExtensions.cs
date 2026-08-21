using DigitalDevServices.Services.Applications;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class DeployableApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDeployableApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
        return services;
    }
}
