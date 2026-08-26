using DigitalDevServices.Data;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Environments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class RemoteEnvironmentRegistrationMapperTests
{
    [TestMethod]
    public void BuildManualDeploymentPrefill_ResolvesPhysicalPathFromTemplate()
    {
        using var fixture = MapperFixture.CreateSync();
        var application = fixture.DeployableApplicationService.CreateAsync(
            "portal",
            isWebApp: true,
            pathToPhysicalPath: @"C:\inetpub\wwwroot\{AppName}",
            pathToLogFiles: @"{PhysicalPath}\Logs").GetAwaiter().GetResult();

        var prefill = fixture.Mapper.BuildManualDeploymentPrefill(
            new RemoteEnvironmentDetails
            {
                Id = 1,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                WebSites =
                [
                    new EnvironmentWebSite
                    {
                        Name = "Default Web Site",
                        MachineName = "UAT-01-APP",
                        WebApplications =
                        [
                            new EnvironmentWebApplication
                            {
                                Path = "/portal"
                            }
                        ]
                    }
                ]
            },
            application);

        Assert.AreEqual(@"C:\inetpub\wwwroot\portal", prefill.PhysicalPath);
        Assert.AreEqual(@"C:\inetpub\wwwroot\portal\Logs", prefill.LogPath);
    }

    [TestMethod]
    public void BuildManualDeploymentPrefill_ResolvesLogPathFromMatchingWebApplication()
    {
        using var fixture = MapperFixture.CreateSync();
        var application = fixture.DeployableApplicationService.CreateAsync(
            "portal",
            isWebApp: true,
            pathToLogFiles: @"{MachineName}\{AppName}\Logs").GetAwaiter().GetResult();

        var prefill = fixture.Mapper.BuildManualDeploymentPrefill(
            new RemoteEnvironmentDetails
            {
                Id = 1,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                WebSites =
                [
                    new EnvironmentWebSite
                    {
                        Name = "Default Web Site",
                        MachineName = "UAT-01-APP",
                        WebApplications =
                        [
                            new EnvironmentWebApplication
                            {
                                Path = "/portal",
                                PhysicalPath = @"C:\inetpub\wwwroot\portal"
                            }
                        ]
                    }
                ]
            },
            application,
            new RemoteEnvironmentDeploymentDetails
            {
                BuildsSuccessful =
                [
                    new EnvironmentBuild
                    {
                        EnvironmentPipelineBuildNumber = 123456,
                        Name = "portal",
                        Parameters =
                        [
                            new EnvironmentBuildParameter
                            {
                                Name = "WipBranch",
                                Value = "feature/123456-portal"
                            }
                        ]
                    }
                ]
            });

        Assert.AreEqual(@"C:\inetpub\wwwroot\portal", prefill.PhysicalPath);
        Assert.AreEqual(@"UAT-01-APP\portal\Logs", prefill.LogPath);
        Assert.AreEqual("123456", prefill.BuildNumber);
        Assert.AreEqual("feature/123456-portal", prefill.SourceBranch);
    }

    [TestMethod]
    public async Task BuildFromEnvironmentUrlAsync_ResolvesLogPathWhenMatchingWebApplicationExists()
    {
        await using var fixture = await MapperFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(45);
        await fixture.DeployableApplicationService.CreateAsync(
            "Customer Portal",
            isWebApp: true,
            pathToLogFiles: @"{MachineName}\{AppName}\Logs");

        var prefill = await fixture.Mapper.BuildFromEnvironmentUrlAsync(
            environment.Id,
            new RemoteEnvironmentDetails
            {
                Id = 45,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                EnvironmentUrls =
                [
                    new EnvironmentUrl
                    {
                        ApplicationName = "Customer Portal",
                        Url = "https://uat-01.example.com/portal"
                    }
                ],
                WebSites =
                [
                    new EnvironmentWebSite
                    {
                        Name = "Default Web Site",
                        MachineName = "UAT-01-APP",
                        WebApplications =
                        [
                            new EnvironmentWebApplication
                            {
                                Path = "/portal",
                                PhysicalPath = @"C:\inetpub\wwwroot\CustomerPortal"
                            }
                        ]
                    }
                ]
            },
            new EnvironmentUrl
            {
                ApplicationName = "Customer Portal",
                Url = "https://uat-01.example.com/portal"
            },
            new RemoteEnvironmentDeploymentDetails
            {
                BuildsSuccessful =
                [
                    new EnvironmentBuild
                    {
                        EnvironmentPipelineBuildNumber = 123456,
                        Name = "Customer Portal",
                        Parameters =
                        [
                            new EnvironmentBuildParameter
                            {
                                Name = "WipBranch",
                                Value = "feature/123456-customer-portal"
                            }
                        ]
                    }
                ]
            });

        Assert.AreEqual(@"UAT-01-APP\Customer Portal\Logs", prefill.Instance.LogPath);
        Assert.AreEqual("https://uat-01.example.com/portal", prefill.Instance.HomepageUrl);
        Assert.AreEqual("123456", prefill.Instance.BuildNumber);
        Assert.AreEqual("feature/123456-customer-portal", prefill.Instance.SourceBranch);
    }

    [TestMethod]
    public async Task BuildFromEnvironmentUrlAsync_WhenApplicationMissing_ReturnsApplicationAndInstancePrefill()
    {
        await using var fixture = await MapperFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(41);
        var details = new RemoteEnvironmentDetails
        {
            Id = 41,
            Code = "UAT-01",
            Name = "UAT-01",
            EnvironmentType = "UAT"
        };

        var prefill = await fixture.Mapper.BuildFromEnvironmentUrlAsync(
            environment.Id,
            details,
            new EnvironmentUrl
            {
                ApplicationName = "Customer Portal",
                Url = "https://uat-01.example.com/portal"
            });

        Assert.IsTrue(prefill.RequiresApplicationCreate);
        Assert.AreEqual("Customer Portal", prefill.Application!.Name);
        Assert.IsTrue(prefill.Application.IsWebApp);
        Assert.AreEqual("https://uat-01.example.com/portal", prefill.Instance.HomepageUrl);
        Assert.IsNull(prefill.Instance.DeployableApplicationId);
    }

    [TestMethod]
    public async Task BuildFromEnvironmentUrlAsync_WhenApplicationExists_ResolvesLogPathFromTemplate()
    {
        await using var fixture = await MapperFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(42);
        await fixture.DeployableApplicationService.CreateAsync(
            "Customer Portal",
            isWebApp: true,
            pathToLogFiles: @"\\{MachineName}\{EnvironmentCode}\{AppName}\Logs");

        var details = new RemoteEnvironmentDetails
        {
            Id = 42,
            Code = "UAT-01",
            Name = "UAT-01",
            EnvironmentType = "UAT"
        };

        var prefill = await fixture.Mapper.BuildFromEnvironmentUrlAsync(
            environment.Id,
            details,
            new EnvironmentUrl
            {
                ApplicationName = "Customer Portal",
                Url = "https://uat-01.example.com/portal"
            });

        Assert.IsFalse(prefill.RequiresApplicationCreate);
        Assert.IsNull(prefill.Application);
        Assert.IsNotNull(prefill.Instance.DeployableApplicationId);
        Assert.AreEqual("https://uat-01.example.com/portal", prefill.Instance.HomepageUrl);
    }

    [TestMethod]
    public async Task BuildFromWebApplicationAsync_WhenApplicationExists_UsesPhysicalPathAndResolvesLogPath()
    {
        await using var fixture = await MapperFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(43);
        await fixture.DeployableApplicationService.CreateAsync(
            "portal",
            isWebApp: true,
            pathToLogFiles: @"{MachineName}\{AppName}\Logs");

        var details = new RemoteEnvironmentDetails
        {
            Id = 43,
            Code = "UAT-01",
            Name = "UAT-01",
            EnvironmentType = "UAT"
        };

        var prefill = await fixture.Mapper.BuildFromWebApplicationAsync(
            environment.Id,
            details,
            new EnvironmentWebSite
            {
                Name = "Default Web Site",
                MachineName = "UAT-01-APP"
            },
            new EnvironmentWebApplication
            {
                ApplicationPoolName = "CustomerPortalAppPool",
                Path = "/portal",
                PhysicalPath = @"C:\inetpub\wwwroot\CustomerPortal"
            });

        Assert.IsFalse(prefill.RequiresApplicationCreate);
        Assert.AreEqual(@"C:\inetpub\wwwroot\CustomerPortal", prefill.Instance.PhysicalPath);
        Assert.AreEqual(@"UAT-01-APP\portal\Logs", prefill.Instance.LogPath);
    }

    [TestMethod]
    public async Task BuildFromEnvironmentUrlAsync_WhenInstanceExists_MarksUpdateAndPreservesBuildNumber()
    {
        await using var fixture = await MapperFixture.CreateAsync();
        var environment = await fixture.CreateTrackedEnvironmentAsync(44);
        var application = await fixture.DeployableApplicationService.CreateAsync("Admin API", isWebApp: true);

        await fixture.ApplicationInstanceService.UpsertAsync(new Model.Applications.ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildNumber = "2.4.1",
            HomepageUrl = "https://old.example.com"
        });

        var prefill = await fixture.Mapper.BuildFromEnvironmentUrlAsync(
            environment.Id,
            new RemoteEnvironmentDetails
            {
                Id = 44,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT"
            },
            new EnvironmentUrl
            {
                ApplicationName = "Admin API",
                Url = "https://uat-01.example.com/api"
            });

        Assert.IsTrue(prefill.IsUpdate);
        Assert.AreEqual("2.4.1", prefill.Instance.BuildNumber);
        Assert.AreEqual("https://uat-01.example.com/api", prefill.Instance.HomepageUrl);
    }

    private sealed class MapperFixture : IAsyncDisposable, IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private MapperFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IRemoteEnvironmentRegistrationMapper mapper,
            IDeployableApplicationService deployableApplicationService,
            IApplicationInstanceService applicationInstanceService)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Mapper = mapper;
            DeployableApplicationService = deployableApplicationService;
            ApplicationInstanceService = applicationInstanceService;
        }

        public DevDashDbContext Db { get; }

        public IRemoteEnvironmentRegistrationMapper Mapper { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public IApplicationInstanceService ApplicationInstanceService { get; }

        public static MapperFixture CreateSync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddDeployableApplicationServices();
            services.AddScoped<IRemoteEnvironmentRegistrationMapper, RemoteEnvironmentRegistrationMapper>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            db.Database.OpenConnection();
            db.Database.EnsureCreated();

            return new MapperFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IRemoteEnvironmentRegistrationMapper>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                serviceProvider.GetRequiredService<IApplicationInstanceService>());
        }

        public static async Task<MapperFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddDeployableApplicationServices();
            services.AddScoped<IRemoteEnvironmentRegistrationMapper, RemoteEnvironmentRegistrationMapper>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            return new MapperFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IRemoteEnvironmentRegistrationMapper>(),
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

        public async ValueTask DisposeAsync() => await _serviceProvider.DisposeAsync();

        public void Dispose() => _serviceProvider.Dispose();
    }
}
