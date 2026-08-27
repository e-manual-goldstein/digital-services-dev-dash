using DigitalDevServices.Data;
using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Environments;
using DigitalDevServices.Services.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class LogPathResolutionServiceTests
{
    [TestMethod]
    public async Task EnsureLogPathAsync_ReturnsExistingPathWithoutRefresh()
    {
        await using var fixture = await LogPathResolutionFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(remoteId: 41, machineName: "UAT-01-APP");
        var application = await fixture.DeployableApplicationService.CreateAsync(
            "portal",
            pathToLogFiles: @"{MachineName}\{AppName}\Logs");
        var instance = await fixture.CreateInstanceAsync(
            application.Id,
            environment.Id,
            logPath: @"C:\logs\portal\app.log");

        var result = await fixture.ResolutionService.EnsureLogPathAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(@"C:\logs\portal\app.log", result.LogPath);
        Assert.IsFalse(result.RefreshedEnvironment);
        Assert.AreEqual(0, fixture.EnvironmentService.RefreshCallCount);
    }

    [TestMethod]
    public async Task EnsureLogPathAsync_ResolvesFromTemplateUsingCachedEnvironment()
    {
        await using var fixture = await LogPathResolutionFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(remoteId: 42, machineName: "UAT-01-APP");
        var application = await fixture.DeployableApplicationService.CreateAsync(
            "portal",
            pathToLogFiles: @"{MachineName}\{AppName}\Logs");
        var instance = await fixture.CreateInstanceAsync(application.Id, environment.Id, logPath: null);

        var result = await fixture.ResolutionService.EnsureLogPathAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(@"UAT-01-APP\portal\Logs", result.LogPath);
        Assert.IsFalse(result.RefreshedEnvironment);
        Assert.AreEqual(0, fixture.EnvironmentService.RefreshCallCount);

        var reloaded = await fixture.ApplicationInstanceService.GetByIdAsync(instance.Id);
        Assert.AreEqual(@"UAT-01-APP\portal\Logs", reloaded!.LogPath);
    }

    [TestMethod]
    public async Task EnsureLogPathAsync_RefreshesEnvironmentWhenTokensMissing()
    {
        await using var fixture = await LogPathResolutionFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(remoteId: 43, machineName: null);
        var application = await fixture.DeployableApplicationService.CreateAsync(
            "portal",
            pathToLogFiles: @"{MachineName}\{AppName}\Logs");
        var instance = await fixture.CreateInstanceAsync(application.Id, environment.Id, logPath: null);

        fixture.EnvironmentService.RefreshedDetails = fixture.CreateEnvironmentDetails(
            remoteId: 43,
            machineName: "UAT-01-APP");

        var result = await fixture.ResolutionService.EnsureLogPathAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(@"UAT-01-APP\portal\Logs", result.LogPath);
        Assert.IsTrue(result.RefreshedEnvironment);
        Assert.AreEqual(1, fixture.EnvironmentService.RefreshCallCount);
    }

    [TestMethod]
    public async Task EnsureLogPathAsync_ReturnsErrorWhenTemplateMissing()
    {
        await using var fixture = await LogPathResolutionFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(remoteId: 44, machineName: "UAT-01-APP");
        var application = await fixture.DeployableApplicationService.CreateAsync("portal");
        var instance = await fixture.CreateInstanceAsync(application.Id, environment.Id, logPath: null);

        var result = await fixture.ResolutionService.EnsureLogPathAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "PathToLogFiles");
    }

    [TestMethod]
    public async Task EnsureLogPathAsync_ReturnsErrorWhenInstanceMissing()
    {
        await using var fixture = await LogPathResolutionFixture.CreateAsync();

        var result = await fixture.ResolutionService.EnsureLogPathAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "not found");
    }

    [TestMethod]
    public async Task EnsureLogPathAsync_ResolvesFromTemplateUsingMatchingWindowsService()
    {
        await using var fixture = await LogPathResolutionFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(
            remoteId: 45,
            machineName: null,
            windowsServices:
            [
                new EnvironmentWindowsService
                {
                    MachineName = "UAT-01-APP",
                    DisplayName = "Digital Services Worker",
                    BinaryPathName = @"C:\Services\DigitalServices.Worker.exe"
                }
            ]);
        var application = await fixture.DeployableApplicationService.CreateAsync(
            "Digital Services Worker",
            isWebApp: false,
            pathToLogFiles: @"\\{MachineName}\{AppName}\Logs");
        var instance = await fixture.CreateInstanceAsync(application.Id, environment.Id, logPath: null);

        var result = await fixture.ResolutionService.EnsureLogPathAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(@"\\UAT-01-APP\Digital Services Worker\Logs", result.LogPath);
        Assert.IsFalse(result.RefreshedEnvironment);
    }

    private sealed class LogPathResolutionFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private LogPathResolutionFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            FakeEnvironmentService environmentService,
            ILogPathResolutionService resolutionService,
            IDeployableApplicationService deployableApplicationService,
            IApplicationInstanceService applicationInstanceService)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            EnvironmentService = environmentService;
            ResolutionService = resolutionService;
            DeployableApplicationService = deployableApplicationService;
            ApplicationInstanceService = applicationInstanceService;
        }

        public DevDashDbContext Db { get; }

        public FakeEnvironmentService EnvironmentService { get; }

        public ILogPathResolutionService ResolutionService { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public IApplicationInstanceService ApplicationInstanceService { get; }

        public static async Task<LogPathResolutionFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddDeployableApplicationServices();
            services.AddScoped<IApplicationInstanceService, ApplicationInstanceService>();
            services.AddScoped<ILogPathResolutionService, LogPathResolutionService>();
            services.AddSingleton<IEnvironmentService, FakeEnvironmentService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var environmentService = (FakeEnvironmentService)serviceProvider.GetRequiredService<IEnvironmentService>();

            return new LogPathResolutionFixture(
                serviceProvider,
                db,
                environmentService,
                serviceProvider.GetRequiredService<ILogPathResolutionService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                serviceProvider.GetRequiredService<IApplicationInstanceService>());
        }

        public async Task<TrackedEnvironment> CreateTrackedEnvironmentAsync(
            int remoteId,
            string? machineName,
            IReadOnlyList<EnvironmentWindowsService>? windowsServices = null)
        {
            var environment = new TrackedEnvironment
            {
                Id = Guid.NewGuid(),
                RemoteId = remoteId,
                DateLastUpdated = DateTimeOffset.UtcNow
            };

            Db.TrackedEnvironments.Add(environment);
            await Db.SaveChangesAsync();

            EnvironmentService.SetCached(new CachedEnvironment
            {
                LocalId = environment.Id,
                RemoteId = remoteId,
                IsFavourite = false,
                Details = CreateEnvironmentDetails(remoteId, machineName, windowsServices),
                DateLastUpdated = DateTimeOffset.UtcNow,
                IsFromCache = true
            });

            return environment;
        }

        public RemoteEnvironmentDetails CreateEnvironmentDetails(
            int remoteId,
            string? machineName,
            IReadOnlyList<EnvironmentWindowsService>? windowsServices = null) =>
            new()
            {
                Id = remoteId,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                WebSites = string.IsNullOrWhiteSpace(machineName)
                    ? []
                    :
                    [
                        new EnvironmentWebSite
                        {
                            Name = "Default Web Site",
                            MachineName = machineName,
                            WebApplications =
                            [
                                new EnvironmentWebApplication
                                {
                                    Path = "/portal",
                                    PhysicalPath = @"C:\inetpub\wwwroot\portal"
                                }
                            ]
                        }
                    ],
                WindowsServices = windowsServices?.ToArray() ?? []
            };

        public async Task<ApplicationInstance> CreateInstanceAsync(
            Guid deployableApplicationId,
            Guid environmentId,
            string? logPath)
        {
            return await ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
            {
                DeployableApplicationId = deployableApplicationId,
                EnvironmentId = environmentId,
                BuildVersionNumber = "1.0.0",
                LogPath = logPath
            });
        }

        public async ValueTask DisposeAsync() => await _serviceProvider.DisposeAsync();
    }

    private sealed class FakeEnvironmentService : IEnvironmentService
    {
        private CachedEnvironment? _cached;

        public int RefreshCallCount { get; private set; }

        public RemoteEnvironmentDetails? RefreshedDetails { get; set; }

        public void SetCached(CachedEnvironment cached) => _cached = cached;

        public Task<IReadOnlyList<CachedEnvironment>> GetEnvironmentsAsync(
            bool forceRefresh = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CachedEnvironment>>(_cached is null ? [] : [_cached]);

        public Task<CachedEnvironment?> GetTrackedEnvironmentAsync(
            Guid localId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_cached?.LocalId == localId ? _cached : null);

        public Task<CachedEnvironment> RefreshEnvironmentAsync(
            int remoteId,
            CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;
            if (_cached is null || _cached.RemoteId != remoteId)
            {
                throw new InvalidOperationException("Environment is not tracked.");
            }

            _cached = _cached with
            {
                Details = RefreshedDetails ?? _cached.Details,
                IsFromCache = false,
                DateLastUpdated = DateTimeOffset.UtcNow
            };

            return Task.FromResult(_cached);
        }

        public Task<CachedEnvironment> SetFavouriteAsync(
            Guid localId,
            bool isFavourite,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UntrackEnvironmentAsync(Guid localId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
