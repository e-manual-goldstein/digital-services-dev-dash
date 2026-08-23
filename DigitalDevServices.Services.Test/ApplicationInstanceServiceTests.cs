using DigitalDevServices.Data;
using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.PipelineFeeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class ApplicationInstanceServiceTests
{
    [TestMethod]
    public async Task UpsertAsync_CreatesInstanceQueryableByEnvironment()
    {
        await using var fixture = await ApplicationInstanceServiceFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Customer Portal API");
        var environment = await fixture.CreateTrackedEnvironmentAsync(16);
        var feed = await fixture.PipelineFeedService.CreateAsync("Feature 123456");

        var upsert = new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildNumber = "1.2.3",
            PipelineFeedId = feed.Id,
            SourceBranch = "feature/123456-foo",
            DeployedAt = new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero),
            PhysicalPath = @"D:\apps\customer-portal",
            LogPath = @"D:\logs\customer-portal.log",
            SqlServerInstance = @"UAT-01\SQL2019"
        };

        var created = await fixture.Service.UpsertAsync(upsert);
        var byEnvironment = await fixture.Service.GetByEnvironmentIdAsync(environment.Id);

        Assert.HasCount(1, byEnvironment);
        Assert.AreEqual(created.Id, byEnvironment[0].Id);
        Assert.AreEqual("1.2.3", byEnvironment[0].BuildNumber);
        Assert.AreEqual("feature/123456-foo", byEnvironment[0].SourceBranch);
        Assert.AreEqual(@"D:\apps\customer-portal", byEnvironment[0].PhysicalPath);
        Assert.AreEqual(feed.Id, byEnvironment[0].PipelineFeedId);
        Assert.AreEqual("Customer Portal API", byEnvironment[0].DeployableApplication.Name);
    }

    [TestMethod]
    public async Task UpsertAsync_UpdatesExistingApplicationEnvironmentSlot()
    {
        await using var fixture = await ApplicationInstanceServiceFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Reporting Service");
        var environment = await fixture.CreateTrackedEnvironmentAsync(42);

        await fixture.Service.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildNumber = "1.0.0"
        });

        var updated = await fixture.Service.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildNumber = "1.0.1",
            SourceBranch = "main"
        });

        Assert.AreEqual("1.0.1", updated.BuildNumber);
        Assert.AreEqual("main", updated.SourceBranch);
        Assert.AreEqual(1, await fixture.Db.ApplicationInstances.CountAsync());
    }

    [TestMethod]
    public async Task DeleteDeployableApplication_IsBlockedWhenInstancesExist()
    {
        await using var fixture = await ApplicationInstanceServiceFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Admin Portal");
        var environment = await fixture.CreateTrackedEnvironmentAsync(7);

        await fixture.Service.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildNumber = "2.0.0"
        });

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.DeployableApplicationService.DeleteAsync(application.Id));

        StringAssert.Contains(ex.Message, "application instances");
    }

    private sealed class ApplicationInstanceServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private ApplicationInstanceServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IApplicationInstanceService service,
            IDeployableApplicationService deployableApplicationService,
            IPipelineFeedService pipelineFeedService)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
            DeployableApplicationService = deployableApplicationService;
            PipelineFeedService = pipelineFeedService;
        }

        public DevDashDbContext Db { get; }

        public IApplicationInstanceService Service { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public IPipelineFeedService PipelineFeedService { get; }

        public static async Task<ApplicationInstanceServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
            services.AddScoped<IPipelineFeedService, PipelineFeedService>();
            services.AddScoped<IApplicationInstanceService, ApplicationInstanceService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            return new ApplicationInstanceServiceFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IApplicationInstanceService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                serviceProvider.GetRequiredService<IPipelineFeedService>());
        }

        public async Task<TrackedEnvironment> CreateTrackedEnvironmentAsync(int remoteId)
        {
            var environment = new TrackedEnvironment
            {
                Id = Guid.NewGuid(),
                RemoteId = remoteId,
                DateLastUpdated = DateTimeOffset.UtcNow
            };

            Db.TrackedEnvironments.Add(environment);
            await Db.SaveChangesAsync();
            return environment;
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
