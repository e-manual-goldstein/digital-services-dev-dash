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
        EnsureDeployableApplicationsTableExists(db);
        EnsureApplicationInstancesTableExists(db);
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

    private static void EnsureDeployableApplicationsTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "DeployableApplications" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_DeployableApplications" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "ProjectKey" TEXT NULL,
                "Notes" TEXT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DeployableApplications_Name" ON "DeployableApplications" ("Name");
            """);
    }

    private static void EnsureApplicationInstancesTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "ApplicationInstances" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ApplicationInstances" PRIMARY KEY,
                "DeployableApplicationId" TEXT NOT NULL,
                "EnvironmentId" TEXT NOT NULL,
                "BuildNumber" TEXT NOT NULL,
                "PipelineFeedId" TEXT NULL,
                "SourceBranch" TEXT NULL,
                "DeployedAt" TEXT NULL,
                "PhysicalPath" TEXT NULL,
                "LogPath" TEXT NULL,
                "SqlServerInstance" TEXT NULL,
                "Notes" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NULL,
                FOREIGN KEY("DeployableApplicationId") REFERENCES "DeployableApplications" ("Id") ON DELETE RESTRICT,
                FOREIGN KEY("EnvironmentId") REFERENCES "TrackedEnvironments" ("Id") ON DELETE RESTRICT,
                FOREIGN KEY("PipelineFeedId") REFERENCES "PipelineFeeds" ("Id") ON DELETE SET NULL
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ApplicationInstances_DeployableApplicationId_EnvironmentId"
            ON "ApplicationInstances" ("DeployableApplicationId", "EnvironmentId");
            """);
    }
}
