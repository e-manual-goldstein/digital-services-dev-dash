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

        public int ListCallCount { get; private set; }

        public string? LastRequestedEnvironmentCode { get; private set; }

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
