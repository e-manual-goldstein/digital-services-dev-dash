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
        EnsurePipelineFeedsTableExists(db);
    }

    private static void EnsurePipelineFeedsTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "PipelineFeeds" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_PipelineFeeds" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Description" TEXT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PipelineFeeds_Name" ON "PipelineFeeds" ("Name");
            """);
    }
}
