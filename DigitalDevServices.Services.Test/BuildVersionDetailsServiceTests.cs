using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class BuildVersionDetailsServiceTests
{
    [TestMethod]
    public async Task GetBuildVersionDetailsAsync_CachesResultsPerBuildNumber()
    {
        var fakeApi = new FakeBuildVersionApiClient
        {
            DetailsByBuildNumber =
            {
                [123456] = new RemoteBuildVersionDetails
                {
                    BuildNumber = 123456,
                    FromShaId = "abc123",
                    Project = "DigitalServices/CustomerPortal",
                    SourceBranch = "feature/test"
                }
            }
        };

        await using var fixture = BuildVersionDetailsServiceFixture.Create(fakeApi);

        var first = await fixture.Service.GetBuildVersionDetailsAsync(123456);
        var second = await fixture.Service.GetBuildVersionDetailsAsync(123456);

        Assert.IsNotNull(first);
        Assert.AreEqual("abc123", first!.FromShaId);
        Assert.AreEqual(first.FromShaId, second!.FromShaId);
        Assert.AreEqual(1, fakeApi.CallCount);
    }

    private sealed class BuildVersionDetailsServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private BuildVersionDetailsServiceFixture(ServiceProvider serviceProvider, IBuildVersionDetailsService service)
        {
            _serviceProvider = serviceProvider;
            Service = service;
        }

        public IBuildVersionDetailsService Service { get; }

        public static BuildVersionDetailsServiceFixture Create(FakeBuildVersionApiClient apiClient)
        {
            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddSingleton<IRemoteEnvironmentApiClient>(apiClient);
            services.AddSingleton<IBuildVersionDetailsService, BuildVersionDetailsService>();
            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<IBuildVersionDetailsService>();
            return new BuildVersionDetailsServiceFixture(serviceProvider, service);
        }

        public async ValueTask DisposeAsync() => await _serviceProvider.DisposeAsync();
    }

    private sealed class FakeBuildVersionApiClient : IRemoteEnvironmentApiClient
    {
        public Dictionary<int, RemoteBuildVersionDetails> DetailsByBuildNumber { get; } = new();

        public int CallCount { get; private set; }

        public Task<RemoteBuildVersionDetails?> GetBuildVersionDetailsAsync(
            int buildNumber,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            DetailsByBuildNumber.TryGetValue(buildNumber, out var details);
            return Task.FromResult(details);
        }

        public Task<RemoteEnvironmentDetails?> GetEnvironmentAsync(string environmentCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<RemoteEnvironmentDetails?>(null);

        public Task<RemoteEnvironmentDeploymentDetails?> GetDeploymentDetailsForEnvironmentAsync(
            string environmentCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RemoteEnvironmentDeploymentDetails?>(null);

        public Task<IReadOnlyList<RemoteEnvironmentDetails>> ListEnvironmentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RemoteEnvironmentDetails>>([]);
    }
}
