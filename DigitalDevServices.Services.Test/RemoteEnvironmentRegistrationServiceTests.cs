using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Environments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class RemoteEnvironmentRegistrationServiceTests
{
    [TestMethod]
    public async Task RegisterFromEnvironmentUrlAsync_CreatesDeployableApplicationAndInstance()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(21);

        var instance = await fixture.Service.RegisterFromEnvironmentUrlAsync(
            environment.Id,
            new EnvironmentUrl
            {
                ApplicationName = "Customer Portal",
                Url = "https://uat-01.example.com/portal"
            });

        Assert.AreEqual("Customer Portal", instance.DeployableApplication.Name);
        Assert.IsTrue(instance.DeployableApplication.IsWebApp);
        Assert.AreEqual("https://uat-01.example.com/portal", instance.HomepageUrl);
        Assert.AreEqual("0", instance.BuildVersionNumber);
        Assert.AreEqual(1, await fixture.Db.DeployableApplications.CountAsync());
        Assert.AreEqual(1, await fixture.Db.ApplicationInstances.CountAsync());
    }

    [TestMethod]
    public async Task RegisterFromEnvironmentUrlAsync_UpdatesExistingInstanceHomepageUrl()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(22);
        var application = await fixture.DeployableApplicationService.CreateAsync("Admin API", isWebApp: true);

        await fixture.ApplicationInstanceService.UpsertAsync(new Model.Applications.ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "2.4.1",
            HomepageUrl = "https://old.example.com"
        });

        var updated = await fixture.Service.RegisterFromEnvironmentUrlAsync(
            environment.Id,
            new EnvironmentUrl
            {
                ApplicationName = "Admin API",
                Url = "https://uat-01.example.com/api"
            });

        Assert.AreEqual("2.4.1", updated.BuildVersionNumber);
        Assert.AreEqual("https://uat-01.example.com/api", updated.HomepageUrl);
        Assert.AreEqual(1, await fixture.Db.ApplicationInstances.CountAsync());
    }

    [TestMethod]
    public async Task RegisterFromEnvironmentUrlAsync_PromotesExistingDeployableApplicationToWebApp()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(23);
        var application = await fixture.DeployableApplicationService.CreateAsync("Legacy Service", isWebApp: false);

        var instance = await fixture.Service.RegisterFromEnvironmentUrlAsync(
            environment.Id,
            new EnvironmentUrl
            {
                ApplicationName = "Legacy Service",
                Url = "https://uat-01.example.com/legacy"
            });

        Assert.IsTrue(instance.DeployableApplication.IsWebApp);
        var refreshedApp = await fixture.DeployableApplicationService.GetByIdAsync(application.Id);
        Assert.IsNotNull(refreshedApp);
        Assert.IsTrue(refreshedApp!.IsWebApp);
    }

    [TestMethod]
    public async Task RegisterFromWebApplicationAsync_CreatesDeployableApplicationAndInstanceFromPathSegment()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(31);

        var instance = await fixture.Service.RegisterFromWebApplicationAsync(
            environment.Id,
            new EnvironmentWebApplication
            {
                ApplicationPoolName = "CustomerPortalAppPool",
                Path = "/portal",
                PhysicalPath = @"C:\inetpub\wwwroot\CustomerPortal"
            });

        Assert.AreEqual("portal", instance.DeployableApplication.Name);
        Assert.IsTrue(instance.DeployableApplication.IsWebApp);
        Assert.AreEqual(@"C:\inetpub\wwwroot\CustomerPortal", instance.PhysicalPath);
        Assert.AreEqual("0", instance.BuildVersionNumber);
    }

    [TestMethod]
    public async Task RegisterFromWebApplicationAsync_UsesApplicationPoolWhenPathIsEmpty()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(32);

        var instance = await fixture.Service.RegisterFromWebApplicationAsync(
            environment.Id,
            new EnvironmentWebApplication
            {
                ApplicationPoolName = "LegacyAppPool",
                PhysicalPath = @"C:\inetpub\wwwroot\Legacy"
            });

        Assert.AreEqual("LegacyAppPool", instance.DeployableApplication.Name);
        Assert.AreEqual(@"C:\inetpub\wwwroot\Legacy", instance.PhysicalPath);
    }

    [TestMethod]
    public async Task RegisterFromWebApplicationAsync_UpdatesExistingInstancePhysicalPath()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(33);
        var application = await fixture.DeployableApplicationService.CreateAsync("api", isWebApp: true);

        await fixture.ApplicationInstanceService.UpsertAsync(new Model.Applications.ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "3.1.0",
            HomepageUrl = "https://uat-01.example.com/api",
            PhysicalPath = @"C:\old\path"
        });

        var updated = await fixture.Service.RegisterFromWebApplicationAsync(
            environment.Id,
            new EnvironmentWebApplication
            {
                ApplicationPoolName = "AdminApiAppPool",
                Path = "/api",
                PhysicalPath = @"C:\inetpub\wwwroot\AdminApi"
            });

        Assert.AreEqual("3.1.0", updated.BuildVersionNumber);
        Assert.AreEqual("https://uat-01.example.com/api", updated.HomepageUrl);
        Assert.AreEqual(@"C:\inetpub\wwwroot\AdminApi", updated.PhysicalPath);
        Assert.AreEqual(1, await fixture.Db.ApplicationInstances.CountAsync());
    }

    [TestMethod]
    public async Task RegisterFromWindowsServiceAsync_CreatesDeployableApplicationAndInstanceFromDisplayName()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(34);

        var instance = await fixture.Service.RegisterFromWindowsServiceAsync(
            environment.Id,
            new EnvironmentWindowsService
            {
                MachineName = "UAT-01-APP",
                DisplayName = "Digital Services Worker",
                BinaryPathName = @"C:\Services\DigitalServices.Worker.exe"
            });

        Assert.AreEqual("Digital Services Worker", instance.DeployableApplication.Name);
        Assert.IsFalse(instance.DeployableApplication.IsWebApp);
        Assert.AreEqual(@"C:\Services\DigitalServices.Worker.exe", instance.PhysicalPath);
        Assert.AreEqual("0", instance.BuildVersionNumber);
    }

    [TestMethod]
    public async Task RegisterFromWindowsServiceAsync_UpdatesExistingInstancePhysicalPath()
    {
        await using var fixture = await RegistrationFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(35);
        var application = await fixture.DeployableApplicationService.CreateAsync(
            "Digital Services Worker",
            isWebApp: false);

        await fixture.ApplicationInstanceService.UpsertAsync(new Model.Applications.ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "1.2.3",
            PhysicalPath = @"C:\old\path\worker.exe"
        });

        var updated = await fixture.Service.RegisterFromWindowsServiceAsync(
            environment.Id,
            new EnvironmentWindowsService
            {
                DisplayName = "Digital Services Worker",
                BinaryPathName = @"C:\Services\DigitalServices.Worker.exe"
            });

        Assert.AreEqual("1.2.3", updated.BuildVersionNumber);
        Assert.AreEqual(@"C:\Services\DigitalServices.Worker.exe", updated.PhysicalPath);
        Assert.AreEqual(1, await fixture.Db.ApplicationInstances.CountAsync());
    }

    private sealed class RegistrationFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private RegistrationFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IRemoteEnvironmentRegistrationService service,
            IDeployableApplicationService deployableApplicationService,
            IApplicationInstanceService applicationInstanceService)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
            DeployableApplicationService = deployableApplicationService;
            ApplicationInstanceService = applicationInstanceService;
        }

        public DevDashDbContext Db { get; }

        public IRemoteEnvironmentRegistrationService Service { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public IApplicationInstanceService ApplicationInstanceService { get; }

        public static async Task<RegistrationFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddDeployableApplicationServices();
            services.AddScoped<IRemoteEnvironmentRegistrationService, RemoteEnvironmentRegistrationService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            return new RegistrationFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IRemoteEnvironmentRegistrationService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                serviceProvider.GetRequiredService<IApplicationInstanceService>());
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
