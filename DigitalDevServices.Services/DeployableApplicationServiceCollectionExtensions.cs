using DigitalDevServices.Services.Applications;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class DeployableApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDeployableApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
        services.AddScoped<IApplicationInstanceService, ApplicationInstanceService>();
        services.AddScoped<IDeployedPackageService, DeployedPackageService>();
        return services;
    }
}
