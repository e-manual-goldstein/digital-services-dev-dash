using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Data;

public static class DevDashDataServiceCollectionExtensions
{
    public static IServiceCollection AddDevDashData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = DevDashDatabasePaths.ResolveConnectionString(configuration);

        services.AddDbContext<DevDashDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }

    public static void EnsureDevDashDatabaseCreated(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevDashDbContext>();
        db.Database.EnsureCreated();
    }
}
