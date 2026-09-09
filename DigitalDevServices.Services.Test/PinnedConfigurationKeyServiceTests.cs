using DigitalDevServices.Data;
using DigitalDevServices.Services.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class PinnedConfigurationKeyServiceTests
{
    [TestMethod]
    public async Task PinAsync_PersistsKeyWithIncrementingDisplayOrder()
    {
        await using var fixture = await PinnedConfigurationKeyServiceFixture.CreateAsync();

        await fixture.Service.PinAsync("ConnectionStrings:Default");
        await fixture.Service.PinAsync("FeatureFlags:NewCheckout");

        var pinnedKeys = await fixture.Service.GetAllAsync();

        Assert.HasCount(2, pinnedKeys);
        Assert.AreEqual("ConnectionStrings:Default", pinnedKeys[0].Key);
        Assert.AreEqual(0, pinnedKeys[0].DisplayOrder);
        Assert.AreEqual("FeatureFlags:NewCheckout", pinnedKeys[1].Key);
        Assert.AreEqual(1, pinnedKeys[1].DisplayOrder);
    }

    [TestMethod]
    public async Task UnpinAsync_RemovesPinnedKey()
    {
        await using var fixture = await PinnedConfigurationKeyServiceFixture.CreateAsync();
        await fixture.Service.PinAsync("ConnectionStrings:Default");

        await fixture.Service.UnpinAsync("ConnectionStrings:Default");

        Assert.IsEmpty(await fixture.Service.GetAllAsync());
    }

    [TestMethod]
    public async Task PinAsync_IsIdempotentForSameKey()
    {
        await using var fixture = await PinnedConfigurationKeyServiceFixture.CreateAsync();

        await fixture.Service.PinAsync("ConnectionStrings:Default");
        await fixture.Service.PinAsync("connectionstrings:default");

        var pinnedKeys = await fixture.Service.GetAllAsync();

        Assert.HasCount(1, pinnedKeys);
    }

    private sealed class PinnedConfigurationKeyServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private PinnedConfigurationKeyServiceFixture(
            ServiceProvider serviceProvider,
            IPinnedConfigurationKeyService service)
        {
            _serviceProvider = serviceProvider;
            Service = service;
        }

        public IPinnedConfigurationKeyService Service { get; }

        public static async Task<PinnedConfigurationKeyServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IPinnedConfigurationKeyService, PinnedConfigurationKeyService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            return new PinnedConfigurationKeyServiceFixture(
                serviceProvider,
                serviceProvider.GetRequiredService<IPinnedConfigurationKeyService>());
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
