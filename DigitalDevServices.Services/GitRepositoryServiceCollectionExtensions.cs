using DigitalDevServices.Services.GitHistory;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services;

public static class GitRepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddGitHistoryServices(this IServiceCollection services)
    {
        services.AddScoped<IGitRepositoryService, GitRepositoryService>();
        return services;
    }
}
