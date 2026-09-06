using DigitalDevServices.Data;
using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class ConfigurationImportServiceTests
{
    [TestMethod]
    public async Task RefreshAsync_ImportsFlattenedKeysFromAppsettingsJson()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var configDirectory = await fixture.CreateConfigDirectoryAsync();
        await fixture.WriteAppsettingsAsync(configDirectory, """
            {
              "ConnectionStrings": {
                "Default": "Server=localhost;Database=AppDb;"
              },
              "FeatureFlags": {
                "NewCheckout": true
              }
            }
            """);

        var instance = await fixture.CreateApplicationInstanceAsync(configDirectory);
        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Settings);

        var connectionString = result.Settings.Single(setting => setting.Key == "ConnectionStrings:Default");
        Assert.AreEqual("Server=localhost;Database=AppDb;", connectionString.Value);
        Assert.AreEqual("appsettings.json", connectionString.Source);

        var featureFlag = result.Settings.Single(setting => setting.Key == "FeatureFlags:NewCheckout");
        Assert.AreEqual("true", featureFlag.Value);
    }

    [TestMethod]
    public async Task RefreshAsync_OverridesKeysFromEnvironmentSpecificFile()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var configDirectory = await fixture.CreateConfigDirectoryAsync();
        await fixture.WriteAppsettingsAsync(configDirectory, """
            {
              "ConnectionStrings": {
                "Default": "Server=localhost;Database=AppDb;"
              }
            }
            """);
        await fixture.WriteAppsettingsAsync(
            configDirectory,
            """
            {
              "ConnectionStrings": {
                "Default": "Server=uat-sql;Database=AppDb;"
              }
            }
            """,
            "appsettings.Production.json");

        var instance = await fixture.CreateApplicationInstanceAsync(configDirectory);
        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        var connectionString = result.Settings.Single(setting => setting.Key == "ConnectionStrings:Default");
        Assert.AreEqual("Server=uat-sql;Database=AppDb;", connectionString.Value);
        Assert.AreEqual("appsettings.Production.json", connectionString.Source);
    }

    [TestMethod]
    public async Task RefreshAsync_ReturnsErrorWhenPhysicalPathMissing()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var instance = await fixture.CreateApplicationInstanceAsync(physicalPath: null);

        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "physical path");
    }

    [TestMethod]
    public async Task RefreshAsync_ReturnsErrorWhenFolderDoesNotExist()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var instance = await fixture.CreateApplicationInstanceAsync(@"D:\does-not-exist\config-test");

        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "does not exist");
    }

    [TestMethod]
    public async Task RefreshAsync_ReturnsErrorWhenInstanceNotFound()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();

        var result = await fixture.ImportService.RefreshAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "not found");
    }

    [TestMethod]
    public async Task RefreshAsync_FlattensNestedLoggingKeys()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var configDirectory = await fixture.CreateConfigDirectoryAsync();
        await fixture.WriteAppsettingsAsync(configDirectory, """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning"
                }
              }
            }
            """);

        var instance = await fixture.CreateApplicationInstanceAsync(configDirectory);
        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Information", result.Settings.Single(setting => setting.Key == "Logging:LogLevel:Default").Value);
        Assert.AreEqual("Warning", result.Settings.Single(setting => setting.Key == "Logging:LogLevel:Microsoft.AspNetCore").Value);
    }

    [TestMethod]
    public async Task RefreshAsync_ImportsAppSettingsAndConnectionStringsFromWebConfig()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var configDirectory = await fixture.CreateConfigDirectoryAsync();
        await fixture.WriteConfigFileAsync(configDirectory, "web.config", """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Api:BaseUrl" value="https://uat-01.example.com/api" />
              </appSettings>
              <connectionStrings>
                <add name="Default" connectionString="Server=uat-sql;Database=PortalDb;" />
              </connectionStrings>
            </configuration>
            """);

        var instance = await fixture.CreateApplicationInstanceAsync(configDirectory);
        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("https://uat-01.example.com/api", result.Settings.Single(setting => setting.Key == "Api:BaseUrl").Value);
        Assert.AreEqual("Server=uat-sql;Database=PortalDb;", result.Settings.Single(setting => setting.Key == "ConnectionStrings:Default").Value);
        Assert.AreEqual("web.config", result.Settings.Single(setting => setting.Key == "Api:BaseUrl").Source);
    }

    [TestMethod]
    public async Task RefreshAsync_WebConfigOverridesAppsettingsJson()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var configDirectory = await fixture.CreateConfigDirectoryAsync();
        await fixture.WriteAppsettingsAsync(configDirectory, """
            {
              "ConnectionStrings": {
                "Default": "Server=localhost;Database=AppDb;"
              }
            }
            """);
        await fixture.WriteConfigFileAsync(configDirectory, "web.config", """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <connectionStrings>
                <add name="Default" connectionString="Server=uat-sql;Database=PortalDb;" />
              </connectionStrings>
            </configuration>
            """);

        var instance = await fixture.CreateApplicationInstanceAsync(configDirectory);
        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        var connectionString = result.Settings.Single(setting => setting.Key == "ConnectionStrings:Default");
        Assert.AreEqual("Server=uat-sql;Database=PortalDb;", connectionString.Value);
        Assert.AreEqual("web.config", connectionString.Source);
    }

    [TestMethod]
    public async Task RefreshAsync_ImportsAppConfigAndExeConfig()
    {
        await using var fixture = await ConfigurationImportServiceFixture.CreateAsync();
        var configDirectory = await fixture.CreateConfigDirectoryAsync();
        await fixture.WriteConfigFileAsync(configDirectory, "app.config", """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="ServiceBus:QueueName" value="orders-inbound" />
              </appSettings>
            </configuration>
            """);
        await fixture.WriteConfigFileAsync(configDirectory, "CustomerPortalAPI.exe.config", """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Worker:PollingIntervalSeconds" value="30" />
              </appSettings>
            </configuration>
            """);

        var instance = await fixture.CreateApplicationInstanceAsync(
            configDirectory,
            applicationName: "Customer Portal API");
        var result = await fixture.ImportService.RefreshAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("orders-inbound", result.Settings.Single(setting => setting.Key == "ServiceBus:QueueName").Value);
        Assert.AreEqual("app.config", result.Settings.Single(setting => setting.Key == "ServiceBus:QueueName").Source);
        Assert.AreEqual("30", result.Settings.Single(setting => setting.Key == "Worker:PollingIntervalSeconds").Value);
        Assert.AreEqual("CustomerPortalAPI.exe.config", result.Settings.Single(setting => setting.Key == "Worker:PollingIntervalSeconds").Source);
    }

    private sealed class ConfigurationImportServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly string _tempRoot;

        private ConfigurationImportServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IConfigurationImportService importService,
            IApplicationInstanceService applicationInstanceService,
            IDeployableApplicationService deployableApplicationService,
            string tempRoot)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            ImportService = importService;
            ApplicationInstanceService = applicationInstanceService;
            DeployableApplicationService = deployableApplicationService;
            _tempRoot = tempRoot;
        }

        public DevDashDbContext Db { get; }

        public IConfigurationImportService ImportService { get; }

        public IApplicationInstanceService ApplicationInstanceService { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public static async Task<ConfigurationImportServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
            services.AddScoped<IApplicationInstanceService, ApplicationInstanceService>();
            services.AddScoped<IConfigurationSettingService, ConfigurationSettingService>();
            services.AddScoped<IConfigurationImportService, ConfigurationImportService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var tempRoot = Path.Combine(Path.GetTempPath(), "devdash-config-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            return new ConfigurationImportServiceFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IConfigurationImportService>(),
                serviceProvider.GetRequiredService<IApplicationInstanceService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                tempRoot);
        }

        public Task<string> CreateConfigDirectoryAsync()
        {
            var configDirectory = Path.Combine(_tempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configDirectory);
            return Task.FromResult(configDirectory);
        }

        public async Task WriteAppsettingsAsync(
            string configDirectory,
            string jsonContent,
            string fileName = "appsettings.json")
        {
            var filePath = Path.Combine(configDirectory, fileName);
            await File.WriteAllTextAsync(filePath, jsonContent);
        }

        public async Task WriteConfigFileAsync(
            string configDirectory,
            string fileName,
            string content)
        {
            var filePath = Path.Combine(configDirectory, fileName);
            await File.WriteAllTextAsync(filePath, content);
        }

        public async Task<ApplicationInstance> CreateApplicationInstanceAsync(
            string? physicalPath,
            string applicationName = "Customer Portal API")
        {
            var application = await DeployableApplicationService.CreateAsync(applicationName);
            var environment = new TrackedEnvironment
            {
                Id = Guid.NewGuid(),
                RemoteId = Random.Shared.Next(1, 10000),
                DateLastUpdated = DateTimeOffset.UtcNow
            };

            Db.TrackedEnvironments.Add(environment);
            await Db.SaveChangesAsync();

            return await ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
            {
                DeployableApplicationId = application.Id,
                EnvironmentId = environment.Id,
                BuildVersionNumber = "1.0.0",
                PhysicalPath = physicalPath
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }

            await _serviceProvider.DisposeAsync();
        }
    }
}
