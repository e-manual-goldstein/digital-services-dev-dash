using DigitalDevServices.Data;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class EnvironmentServiceTests
{
    [TestMethod]
    public async Task GetEnvironmentsAsync_LoadsFromRemoteListAndCreatesLocalRecords()
    {
        await using var fixture = await EnvironmentServiceFixture.CreateAsync(new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [42] = new RemoteEnvironmentDetails
                {
                    Id = 42,
                    Code = "UAT-01",
                    Name = "UAT-01",
                    EnvironmentType = "UAT"
                },
                [2] = new RemoteEnvironmentDetails
                {
                    Id = 2,
                    Code = "INT",
                    Name = "Integration",
                    EnvironmentType = "Integration"
                }
            }
        });

        var results = await fixture.Service.GetEnvironmentsAsync();

        Assert.HasCount(2, results);
        Assert.AreEqual("Integration", results[0].Details.Name);
        Assert.AreEqual("UAT-01", results[1].Details.Name);
        Assert.AreEqual(1, fixture.Api.ListCallCount);
        Assert.AreEqual(2, await fixture.Db.TrackedEnvironments.CountAsync());
    }

    [TestMethod]
    public async Task GetEnvironmentsAsync_UsesCatalogCacheUntilForcedRefresh()
    {
        var fakeApi = new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [7] = new RemoteEnvironmentDetails
                {
                    Id = 7,
                    Code = "DEV",
                    Name = "Dev",
                    EnvironmentType = "Development"
                }
            }
        };

        await using var fixture = await EnvironmentServiceFixture.CreateAsync(fakeApi);
        var first = await fixture.Service.GetEnvironmentsAsync();
        Assert.AreEqual(1, fakeApi.ListCallCount);
        Assert.IsFalse(first[0].IsFromCache);

        var cached = await fixture.Service.GetEnvironmentsAsync();
        Assert.AreEqual(1, fakeApi.ListCallCount);
        Assert.IsTrue(cached[0].IsFromCache);

        var refreshed = await fixture.Service.GetEnvironmentsAsync(forceRefresh: true);
        Assert.AreEqual(2, fakeApi.ListCallCount);
        Assert.IsFalse(refreshed[0].IsFromCache);
    }

    [TestMethod]
    public async Task RefreshEnvironmentAsync_FetchesEnvironmentAndDeploymentDetails()
    {
        var fakeApi = new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [99] = new RemoteEnvironmentDetails
                {
                    Id = 99,
                    Code = "QA",
                    Name = "QA",
                    EnvironmentType = "QA"
                }
            },
            DeploymentDetailsByCode =
            {
                ["QA"] = new RemoteEnvironmentDeploymentDetails
                {
                    BuildsSuccessful =
                    [
                        new EnvironmentBuild
                        {
                            EnvironmentPipelineBuildNumber = 42,
                            Name = "QA build",
                            Parameters =
                            [
                                new EnvironmentBuildParameter
                                {
                                    Name = "WipBranch",
                                    Value = "feature/qa"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        await using var fixture = await EnvironmentServiceFixture.CreateAsync(fakeApi);
        await fixture.Service.GetEnvironmentsAsync();
        fakeApi.GetCallCount = 0;
        fakeApi.DeploymentDetailsCallCount = 0;

        var refreshed = await fixture.Service.RefreshEnvironmentAsync(99);

        Assert.AreEqual("QA", refreshed.Details.Name);
        Assert.IsFalse(refreshed.IsFromCache);
        Assert.AreEqual(1, fakeApi.GetCallCount);
        Assert.AreEqual(1, fakeApi.DeploymentDetailsCallCount);
        Assert.AreEqual("QA", fakeApi.LastRequestedEnvironmentCode);
        Assert.IsNotNull(refreshed.DeploymentDetails);
        Assert.AreEqual(42, refreshed.DeploymentDetails!.GetPrimaryBuild()!.EnvironmentPipelineBuildNumber);
        Assert.AreEqual("feature/qa", refreshed.DeploymentDetails.GetPrimaryWipBranch());
    }

    [TestMethod]
    public async Task RefreshEnvironmentAsync_RefreshesSingleEnvironmentFromRemoteApi()
    {
        var fakeApi = new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [99] = new RemoteEnvironmentDetails
                {
                    Id = 99,
                    Code = "QA",
                    Name = "QA",
                    EnvironmentType = "QA"
                }
            }
        };

        await using var fixture = await EnvironmentServiceFixture.CreateAsync(fakeApi);
        await fixture.Service.GetEnvironmentsAsync();
        fakeApi.GetCallCount = 0;

        var refreshed = await fixture.Service.RefreshEnvironmentAsync(99);

        Assert.AreEqual("QA", refreshed.Details.Name);
        Assert.AreEqual("QA", refreshed.Details.Code);
        Assert.IsFalse(refreshed.IsFromCache);
        Assert.AreEqual(1, fakeApi.GetCallCount);
        Assert.AreEqual("QA", fakeApi.LastRequestedEnvironmentCode);
    }

    [TestMethod]
    public async Task SetFavouriteAsync_PersistsToDatabaseAndUpdatesCachedEnvironment()
    {
        var fakeApi = new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [42] = new RemoteEnvironmentDetails
                {
                    Id = 42,
                    Code = "UAT-01",
                    Name = "UAT-01",
                    EnvironmentType = "UAT"
                },
                [2] = new RemoteEnvironmentDetails
                {
                    Id = 2,
                    Code = "INT",
                    Name = "Integration",
                    EnvironmentType = "Integration"
                }
            }
        };

        await using var fixture = await EnvironmentServiceFixture.CreateAsync(fakeApi);
        var environments = await fixture.Service.GetEnvironmentsAsync();
        var uat01 = environments.Single(environment => environment.RemoteId == 42);
        Assert.IsFalse(uat01.IsFavourite);

        var favourited = await fixture.Service.SetFavouriteAsync(uat01.LocalId, isFavourite: true);
        Assert.IsTrue(favourited.IsFavourite);

        var fromService = await fixture.Service.GetTrackedEnvironmentAsync(uat01.LocalId);
        Assert.IsNotNull(fromService);
        Assert.IsTrue(fromService!.IsFavourite);

        var fromCatalog = await fixture.Service.GetEnvironmentsAsync();
        Assert.IsTrue(fromCatalog.Single(environment => environment.LocalId == uat01.LocalId).IsFavourite);

        var tracked = await fixture.Db.TrackedEnvironments
            .AsNoTracking()
            .SingleAsync(environment => environment.Id == uat01.LocalId);
        Assert.IsTrue(tracked.IsFavourite);

        var cleared = await fixture.Service.SetFavouriteAsync(uat01.LocalId, isFavourite: false);
        Assert.IsFalse(cleared.IsFavourite);

        tracked = await fixture.Db.TrackedEnvironments
            .AsNoTracking()
            .SingleAsync(environment => environment.Id == uat01.LocalId);
        Assert.IsFalse(tracked.IsFavourite);
    }

    private sealed class EnvironmentServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private EnvironmentServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IEnvironmentService service,
            FakeRemoteEnvironmentApiClient api)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
            Api = api;
        }

        public DevDashDbContext Db { get; }

        public IEnvironmentService Service { get; }

        public FakeRemoteEnvironmentApiClient Api { get; }

        public static async Task<EnvironmentServiceFixture> CreateAsync(FakeRemoteEnvironmentApiClient apiClient)
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddMemoryCache();
            services.AddSingleton<IRemoteEnvironmentApiClient>(apiClient);
            services.Configure<EnvironmentCacheOptions>(options => options.CacheLifetime = TimeSpan.FromHours(24));
            services.AddScoped<IEnvironmentService, EnvironmentService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var service = serviceProvider.GetRequiredService<IEnvironmentService>();
            return new EnvironmentServiceFixture(serviceProvider, db, service, apiClient);
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    private sealed class FakeRemoteEnvironmentApiClient : IRemoteEnvironmentApiClient
    {
        public Dictionary<int, RemoteEnvironmentDetails> Environments { get; } = new();

        public int GetCallCount { get; set; }

        public int DeploymentDetailsCallCount { get; set; }

        public int ListCallCount { get; private set; }

        public string? LastRequestedEnvironmentCode { get; private set; }

        public Dictionary<string, RemoteEnvironmentDeploymentDetails> DeploymentDetailsByCode { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Task<RemoteEnvironmentDeploymentDetails?> GetDeploymentDetailsForEnvironmentAsync(
            string environmentCode,
            CancellationToken cancellationToken = default)
        {
            DeploymentDetailsCallCount++;
            LastRequestedEnvironmentCode = environmentCode;
            DeploymentDetailsByCode.TryGetValue(environmentCode, out var details);
            return Task.FromResult(details);
        }

        public Task<RemoteBuildVersionDetails?> GetBuildVersionDetailsAsync(
            string environmentPipelineBuildNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RemoteBuildVersionDetails?>(null);

        public Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(
            string environmentCode,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            LastRequestedEnvironmentCode = environmentCode;
            var details = Environments.Values.FirstOrDefault(environment =>
                environment.Code.Equals(environmentCode, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(details);
        }

        public Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(
            CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            IReadOnlyList<RemoteEnvironmentDetails> items = Environments.Values.ToList();
            return Task.FromResult(items);
        }
    }
}
