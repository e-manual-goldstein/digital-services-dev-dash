using DigitalDevServices.Data;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class EnvironmentServiceTests
{
    [TestMethod]
    public async Task TrackEnvironmentAsync_PersistsLocalRecordAndFetchesRemoteDetails()
    {
        await using var fixture = await EnvironmentServiceFixture.CreateAsync(new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [42] = new RemoteEnvironmentDetails
                {
                    RemoteId = 42,
                    Name = "Partial16",
                    SqlServerInstance = "sql-partial16.example"
                }
            }
        });

        var result = await fixture.Service.TrackEnvironmentAsync(42);

        Assert.AreEqual(42, result.RemoteId);
        Assert.AreEqual("Partial16", result.Details.Name);
        Assert.AreEqual("sql-partial16.example", result.Details.SqlServerInstance);
        Assert.IsFalse(result.IsFromCache);

        var stored = await fixture.Db.TrackedEnvironments.SingleAsync();
        Assert.AreEqual(result.LocalId, stored.Id);
        Assert.AreEqual(42, stored.RemoteId);
        Assert.AreEqual(result.DateLastUpdated, stored.DateLastUpdated);
    }

    [TestMethod]
    public async Task GetTrackedEnvironmentAsync_UsesMemoryCacheUntilForcedRefresh()
    {
        var fakeApi = new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [7] = new RemoteEnvironmentDetails
                {
                    RemoteId = 7,
                    Name = "Dev",
                    SqlServerInstance = "sql-dev"
                }
            }
        };

        await using var fixture = await EnvironmentServiceFixture.CreateAsync(fakeApi);
        var tracked = await fixture.Service.TrackEnvironmentAsync(7);
        Assert.AreEqual(1, fakeApi.GetCallCount);

        var cached = await fixture.Service.GetTrackedEnvironmentAsync(tracked.LocalId);
        Assert.IsNotNull(cached);
        Assert.IsTrue(cached!.IsFromCache);
        Assert.AreEqual(1, fakeApi.GetCallCount);

        var refreshed = await fixture.Service.RefreshEnvironmentAsync(tracked.LocalId);
        Assert.IsFalse(refreshed.IsFromCache);
        Assert.AreEqual(2, fakeApi.GetCallCount);
        Assert.IsTrue(refreshed.DateLastUpdated >= tracked.DateLastUpdated);
    }

    [TestMethod]
    public async Task TrackEnvironmentAsync_WithExistingRemoteId_RefreshesSameLocalRecord()
    {
        var fakeApi = new FakeRemoteEnvironmentApiClient
        {
            Environments =
            {
                [99] = new RemoteEnvironmentDetails
                {
                    RemoteId = 99,
                    Name = "QA",
                    SqlServerInstance = "sql-qa"
                }
            }
        };

        await using var fixture = await EnvironmentServiceFixture.CreateAsync(fakeApi);
        var first = await fixture.Service.TrackEnvironmentAsync(99);
        var second = await fixture.Service.TrackEnvironmentAsync(99);

        Assert.AreEqual(first.LocalId, second.LocalId);
        Assert.AreEqual(1, await fixture.Db.TrackedEnvironments.CountAsync());
        Assert.AreEqual(2, fakeApi.GetCallCount);
    }

    private sealed class EnvironmentServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private EnvironmentServiceFixture(ServiceProvider serviceProvider, DevDashDbContext db, IEnvironmentService service)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
        }

        public DevDashDbContext Db { get; }

        public IEnvironmentService Service { get; }

        public static async Task<EnvironmentServiceFixture> CreateAsync(IRemoteEnvironmentApiClient apiClient)
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddMemoryCache();
            services.AddSingleton(apiClient);
            services.Configure<EnvironmentCacheOptions>(options => options.CacheLifetime = TimeSpan.FromHours(24));
            services.AddScoped<IEnvironmentService, EnvironmentService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var service = serviceProvider.GetRequiredService<IEnvironmentService>();
            return new EnvironmentServiceFixture(serviceProvider, db, service);
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    private sealed class FakeRemoteEnvironmentApiClient : IRemoteEnvironmentApiClient
    {
        public Dictionary<int, RemoteEnvironmentDetails> Environments { get; } = new();

        public int GetCallCount { get; private set; }

        public Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(int remoteId, CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            Environments.TryGetValue(remoteId, out var details);
            return Task.FromResult(details);
        }

        public Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RemoteEnvironmentDetails> items = Environments.Values.ToList();
            return Task.FromResult(items);
        }
    }
}
