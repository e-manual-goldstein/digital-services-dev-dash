using DigitalDevServices.Services.PipelineFeeds;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class PipelineFeedServiceCollectionExtensions
{
    public static IServiceCollection AddPipelineFeedServices(this IServiceCollection services)
    {
        services.AddScoped<IPipelineFeedService, PipelineFeedService>();
        return services;
    }
}
