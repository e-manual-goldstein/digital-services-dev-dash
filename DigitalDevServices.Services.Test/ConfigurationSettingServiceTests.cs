using DigitalDevServices.Data;
using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Configuration;
using DigitalDevServices.Services.PipelineFeeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class ConfigurationSettingServiceTests
{
    [TestMethod]
    public async Task UpsertAsync_PersistsMultipleKeysForInstance()
    {
        await using var fixture = await ConfigurationSettingServiceFixture.CreateAsync();
        var instance = await fixture.CreateApplicationInstanceAsync();

        await fixture.Service.UpsertAsync(new ConfigurationSettingUpsert
        {
            ApplicationInstanceId = instance.Id,
            Key = "ConnectionStrings:Default",
            Value = "Server=localhost;Database=AppDb;",
            Source = "appsettings.json"
        });

        await fixture.Service.UpsertAsync(new ConfigurationSettingUpsert
        {
            ApplicationInstanceId = instance.Id,
            Key = "FeatureFlags:NewCheckout",
            Value = "true",
            Source = "appsettings.json"
        });

        var settings = await fixture.Service.GetByApplicationInstanceIdAsync(instance.Id);

        Assert.HasCount(2, settings);
        Assert.AreEqual("ConnectionStrings:Default", settings[0].Key);
        Assert.AreEqual("FeatureFlags:NewCheckout", settings[1].Key);
        Assert.AreEqual("appsettings.json", settings[0].Source);
    }

    [TestMethod]
    public async Task UpsertAsync_UpdatesValueAndCapturedAt()
    {
        await using var fixture = await ConfigurationSettingServiceFixture.CreateAsync();
        var instance = await fixture.CreateApplicationInstanceAsync();

        var created = await fixture.Service.UpsertAsync(new ConfigurationSettingUpsert
        {
            ApplicationInstanceId = instance.Id,
            Key = "Api:BaseUrl",
            Value = "https://uat.example/api",
            Source = "appsettings.json"
        });

        await Task.Delay(5);

        var updated = await fixture.Service.UpsertAsync(new ConfigurationSettingUpsert
        {
            ApplicationInstanceId = instance.Id,
            Key = "Api:BaseUrl",
            Value = "https://uat.example/api/v2",
            Source = "appsettings.Production.json"
        });

        Assert.AreEqual(created.Id, updated.Id);
        Assert.AreEqual("https://uat.example/api/v2", updated.Value);
        Assert.AreEqual("appsettings.Production.json", updated.Source);
        Assert.IsTrue(updated.CapturedAt > created.CapturedAt);

        var loaded = await fixture.Service.GetByApplicationInstanceAndKeyAsync(instance.Id, "Api:BaseUrl");
        Assert.IsNotNull(loaded);
        Assert.AreEqual(updated.CapturedAt, loaded!.CapturedAt);
    }

    [TestMethod]
    public async Task UpsertAsync_ThrowsWhenApplicationInstanceMissing()
    {
        await using var fixture = await ConfigurationSettingServiceFixture.CreateAsync();

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            fixture.Service.UpsertAsync(new ConfigurationSettingUpsert
            {
                ApplicationInstanceId = Guid.NewGuid(),
                Key = "ConnectionStrings:Default",
                Value = "Server=localhost;"
            }));

        StringAssert.Contains(ex.Message, "not found");
    }

    private sealed class ConfigurationSettingServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private ConfigurationSettingServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IConfigurationSettingService service,
            IApplicationInstanceService applicationInstanceService,
            IDeployableApplicationService deployableApplicationService)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
            ApplicationInstanceService = applicationInstanceService;
            DeployableApplicationService = deployableApplicationService;
        }

        public DevDashDbContext Db { get; }

        public IConfigurationSettingService Service { get; }

        public IApplicationInstanceService ApplicationInstanceService { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public static async Task<ConfigurationSettingServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
            services.AddScoped<IPipelineFeedService, PipelineFeedService>();
            services.AddScoped<IApplicationInstanceService, ApplicationInstanceService>();
            services.AddScoped<IConfigurationSettingService, ConfigurationSettingService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            return new ConfigurationSettingServiceFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IConfigurationSettingService>(),
                serviceProvider.GetRequiredService<IApplicationInstanceService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>());
        }

        public async Task<ApplicationInstance> CreateApplicationInstanceAsync()
        {
            var application = await DeployableApplicationService.CreateAsync("Customer Portal API");
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
                PhysicalPath = @"D:\apps\customer-portal"
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
