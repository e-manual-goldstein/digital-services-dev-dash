using DigitalDevServices.Services.Coverlet;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class CoverletServiceCollectionExtensions
{
    public static IServiceCollection AddCoverletServices(this IServiceCollection services)
    {
        services.AddSingleton<ICoverletCoverageReportParser, CoverletCoverageReportParser>();

        return services;
    }
}
