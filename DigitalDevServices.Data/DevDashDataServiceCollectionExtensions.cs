using System.Data;
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
        EnsureDeployableApplicationsIsWebAppColumnExists(db);
        EnsureApplicationInstancesHomepageUrlColumnExists(db);
        EnsureLogFormatProfilesTableExists(db);
        EnsureConfigurationSettingsTableExists(db);
        EnsureTrackedEnvironmentsIsFavouriteColumnExists(db);
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
                "IsWebApp" INTEGER NOT NULL DEFAULT 0,
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
                "HomepageUrl" TEXT NULL,
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

    private static void EnsureDeployableApplicationsIsWebAppColumnExists(DevDashDbContext db)
    {
        EnsureColumnExists(
            db,
            "DeployableApplications",
            "IsWebApp",
            "ALTER TABLE \"DeployableApplications\" ADD COLUMN \"IsWebApp\" INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureApplicationInstancesHomepageUrlColumnExists(DevDashDbContext db)
    {
        EnsureColumnExists(
            db,
            "ApplicationInstances",
            "HomepageUrl",
            "ALTER TABLE \"ApplicationInstances\" ADD COLUMN \"HomepageUrl\" TEXT NULL");
    }

    private static void EnsureLogFormatProfilesTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "LogFormatProfiles" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_LogFormatProfiles" PRIMARY KEY,
                "DeployableApplicationId" TEXT NOT NULL,
                "FormatName" TEXT NOT NULL,
                "ParserConfig" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "UpdatedAt" TEXT NULL,
                FOREIGN KEY("DeployableApplicationId") REFERENCES "DeployableApplications" ("Id") ON DELETE CASCADE
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_LogFormatProfiles_DeployableApplicationId"
            ON "LogFormatProfiles" ("DeployableApplicationId");
            """);
    }

    private static void EnsureConfigurationSettingsTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "ConfigurationSettings" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ConfigurationSettings" PRIMARY KEY,
                "ApplicationInstanceId" TEXT NOT NULL,
                "Key" TEXT NOT NULL,
                "Value" TEXT NOT NULL,
                "Source" TEXT NULL,
                "CapturedAt" TEXT NOT NULL,
                FOREIGN KEY("ApplicationInstanceId") REFERENCES "ApplicationInstances" ("Id") ON DELETE CASCADE
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ConfigurationSettings_ApplicationInstanceId_Key"
            ON "ConfigurationSettings" ("ApplicationInstanceId", "Key");
            """);
    }

    private static void EnsureTrackedEnvironmentsIsFavouriteColumnExists(DevDashDbContext db)
    {
        EnsureColumnExists(
            db,
            "TrackedEnvironments",
            "IsFavourite",
            "ALTER TABLE \"TrackedEnvironments\" ADD COLUMN \"IsFavourite\" INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureColumnExists(
        DevDashDbContext db,
        string tableName,
        string columnName,
        string addColumnSql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        db.Database.ExecuteSqlRaw(addColumnSql);
    }
}
