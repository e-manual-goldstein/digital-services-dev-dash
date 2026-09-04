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
        EnsureTrackedEnvironmentsDisplayOrderColumnExists(db);
        EnsureDeployableApplicationsPathToLogFilesColumnExists(db);
        EnsureDeployableApplicationsPathToPhysicalPathColumnExists(db);
        EnsureGitRepositoriesTableExists(db);
        EnsureArtifactComponentsTableExists(db);
        EnsureHistoricGitRepoRecordsTableExists(db);
        MigrateGitHistoryToArtifactComponents(db);
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

    private static void EnsureDeployableApplicationsPathToLogFilesColumnExists(DevDashDbContext db)
    {
        EnsureColumnExists(
            db,
            "DeployableApplications",
            "PathToLogFiles",
            "ALTER TABLE \"DeployableApplications\" ADD COLUMN \"PathToLogFiles\" TEXT NULL");
    }

    private static void EnsureDeployableApplicationsPathToPhysicalPathColumnExists(DevDashDbContext db)
    {
        EnsureColumnExists(
            db,
            "DeployableApplications",
            "PathToPhysicalPath",
            "ALTER TABLE \"DeployableApplications\" ADD COLUMN \"PathToPhysicalPath\" TEXT NULL");
    }

    private static void EnsureTrackedEnvironmentsIsFavouriteColumnExists(DevDashDbContext db)
    {
        EnsureColumnExists(
            db,
            "TrackedEnvironments",
            "IsFavourite",
            "ALTER TABLE \"TrackedEnvironments\" ADD COLUMN \"IsFavourite\" INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureTrackedEnvironmentsDisplayOrderColumnExists(DevDashDbContext db)
    {
        EnsureColumnExists(
            db,
            "TrackedEnvironments",
            "DisplayOrder",
            "ALTER TABLE \"TrackedEnvironments\" ADD COLUMN \"DisplayOrder\" INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureGitRepositoriesTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "GitRepositories" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_GitRepositories" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_GitRepositories_Name" ON "GitRepositories" ("Name");
            """);
    }

    private static void EnsureArtifactComponentsTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "ArtifactComponents" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ArtifactComponents" PRIMARY KEY,
                "GitRepositoryId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "DateMigrated" TEXT NOT NULL,
                "CurrentLocationUrl" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                FOREIGN KEY("GitRepositoryId") REFERENCES "GitRepositories" ("Id") ON DELETE CASCADE
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ArtifactComponents_GitRepositoryId_Name"
                ON "ArtifactComponents" ("GitRepositoryId", "Name");
            """);
    }

    private static void EnsureHistoricGitRepoRecordsTableExists(DevDashDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "HistoricGitRepoRecords" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_HistoricGitRepoRecords" PRIMARY KEY,
                "ArtifactComponentId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "LastLocationUrl" TEXT NOT NULL,
                "DateMigrated" TEXT NOT NULL,
                FOREIGN KEY("ArtifactComponentId") REFERENCES "ArtifactComponents" ("Id") ON DELETE CASCADE
            );
            """);

        EnsureColumnExists(
            db,
            "HistoricGitRepoRecords",
            "ArtifactComponentId",
            "ALTER TABLE \"HistoricGitRepoRecords\" ADD COLUMN \"ArtifactComponentId\" TEXT NULL");
    }

    private static void MigrateGitHistoryToArtifactComponents(DevDashDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        var hasLegacyRepoColumns = TableHasColumn(connection, "GitRepositories", "CurrentLocationUrl");
        var hasLegacyHistoricFk = TableHasColumn(connection, "HistoricGitRepoRecords", "GitRepositoryId");

        if (!hasLegacyRepoColumns && !hasLegacyHistoricFk)
        {
            return;
        }

        var componentIdsByRepository = new Dictionary<Guid, Guid>();

        if (hasLegacyRepoColumns)
        {
            using var repoCommand = connection.CreateCommand();
            repoCommand.CommandText = """
                SELECT "Id", "Name", "DateMigrated", "CurrentLocationUrl", "CreatedAt"
                FROM "GitRepositories"
                WHERE "CurrentLocationUrl" IS NOT NULL AND TRIM("CurrentLocationUrl") <> ''
                """;

            using var repoReader = repoCommand.ExecuteReader();
            while (repoReader.Read())
            {
                var repositoryId = Guid.Parse(repoReader.GetString(0));
                if (RepositoryHasComponent(connection, repositoryId))
                {
                    var existingComponentId = GetFirstComponentIdForRepository(connection, repositoryId);
                    if (existingComponentId is not null)
                    {
                        componentIdsByRepository[repositoryId] = existingComponentId.Value;
                    }

                    continue;
                }

                var componentId = Guid.NewGuid();
                var repositoryName = repoReader.GetString(1);
                var dateMigrated = repoReader.GetString(2);
                var currentLocationUrl = repoReader.GetString(3);
                var createdAt = repoReader.IsDBNull(4)
                    ? DateTimeOffset.UtcNow.ToString("O")
                    : repoReader.GetString(4);

                InsertArtifactComponent(
                    connection,
                    componentId,
                    repositoryId,
                    repositoryName,
                    dateMigrated,
                    currentLocationUrl,
                    createdAt);

                componentIdsByRepository[repositoryId] = componentId;
            }
        }

        if (!hasLegacyHistoricFk)
        {
            return;
        }

        EnsureColumnExists(
            db,
            "HistoricGitRepoRecords",
            "ArtifactComponentId",
            "ALTER TABLE \"HistoricGitRepoRecords\" ADD COLUMN \"ArtifactComponentId\" TEXT NULL");

        using var historicCommand = connection.CreateCommand();
        historicCommand.CommandText = """
            SELECT "Id", "GitRepositoryId"
            FROM "HistoricGitRepoRecords"
            WHERE "ArtifactComponentId" IS NULL OR TRIM("ArtifactComponentId") = ''
            """;

        using var historicReader = historicCommand.ExecuteReader();
        while (historicReader.Read())
        {
            var recordId = Guid.Parse(historicReader.GetString(0));
            var repositoryId = Guid.Parse(historicReader.GetString(1));

            if (!componentIdsByRepository.TryGetValue(repositoryId, out var componentId))
            {
                componentId = GetFirstComponentIdForRepository(connection, repositoryId)
                    ?? CreatePlaceholderComponent(connection, repositoryId);

                componentIdsByRepository[repositoryId] = componentId;
            }

            UpdateHistoricRecordComponentId(connection, recordId, componentId);
        }
    }

    private static bool TableHasColumn(System.Data.Common.DbConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RepositoryHasComponent(System.Data.Common.DbConnection connection, Guid repositoryId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM "ArtifactComponents"
            WHERE "GitRepositoryId" = $repositoryId
            LIMIT 1
            """;
        AddParameter(command, "$repositoryId", repositoryId.ToString());
        return command.ExecuteScalar() is not null;
    }

    private static Guid? GetFirstComponentIdForRepository(
        System.Data.Common.DbConnection connection,
        Guid repositoryId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Id"
            FROM "ArtifactComponents"
            WHERE "GitRepositoryId" = $repositoryId
            ORDER BY "CreatedAt"
            LIMIT 1
            """;
        AddParameter(command, "$repositoryId", repositoryId.ToString());

        var result = command.ExecuteScalar();
        return result is string id ? Guid.Parse(id) : null;
    }

    private static Guid CreatePlaceholderComponent(System.Data.Common.DbConnection connection, Guid repositoryId)
    {
        using var nameCommand = connection.CreateCommand();
        nameCommand.CommandText = """
            SELECT "Name", "CreatedAt"
            FROM "GitRepositories"
            WHERE "Id" = $repositoryId
            """;
        AddParameter(nameCommand, "$repositoryId", repositoryId.ToString());

        using var reader = nameCommand.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException($"Git repository '{repositoryId}' was not found during migration.");
        }

        var repositoryName = reader.GetString(0);
        var createdAt = reader.IsDBNull(1)
            ? DateTimeOffset.UtcNow.ToString("O")
            : reader.GetString(1);

        var componentId = Guid.NewGuid();
        InsertArtifactComponent(
            connection,
            componentId,
            repositoryId,
            repositoryName,
            createdAt,
            "https://dev.azure.com/",
            createdAt);

        return componentId;
    }

    private static void InsertArtifactComponent(
        System.Data.Common.DbConnection connection,
        Guid componentId,
        Guid repositoryId,
        string name,
        string dateMigrated,
        string currentLocationUrl,
        string createdAt)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "ArtifactComponents" (
                "Id", "GitRepositoryId", "Name", "DateMigrated", "CurrentLocationUrl", "CreatedAt")
            VALUES ($id, $repositoryId, $name, $dateMigrated, $currentLocationUrl, $createdAt)
            """;
        AddParameter(command, "$id", componentId.ToString());
        AddParameter(command, "$repositoryId", repositoryId.ToString());
        AddParameter(command, "$name", name);
        AddParameter(command, "$dateMigrated", dateMigrated);
        AddParameter(command, "$currentLocationUrl", currentLocationUrl);
        AddParameter(command, "$createdAt", createdAt);
        command.ExecuteNonQuery();
    }

    private static void UpdateHistoricRecordComponentId(
        System.Data.Common.DbConnection connection,
        Guid recordId,
        Guid componentId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE "HistoricGitRepoRecords"
            SET "ArtifactComponentId" = $componentId
            WHERE "Id" = $recordId
            """;
        AddParameter(command, "$componentId", componentId.ToString());
        AddParameter(command, "$recordId", recordId.ToString());
        command.ExecuteNonQuery();
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
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
