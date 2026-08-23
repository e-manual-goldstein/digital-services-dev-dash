using DigitalDevServices.Data;
using DigitalDevServices.Services.PipelineFeeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class PipelineFeedServiceTests
{
    [TestMethod]
    public async Task CreateAsync_PersistsFeedAndReadsBackByName()
    {
        await using var fixture = await PipelineFeedServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Feature 123456", "WIP packages for feature work");

        var loaded = await fixture.Service.GetByNameAsync("Feature 123456");

        Assert.IsNotNull(loaded);
        Assert.AreEqual(created.Id, loaded!.Id);
        Assert.AreEqual("Feature 123456", loaded.Name);
        Assert.AreEqual("WIP packages for feature work", loaded.Description);
    }

    [TestMethod]
    public async Task CreateAsync_RejectsDuplicateName()
    {
        await using var fixture = await PipelineFeedServiceFixture.CreateAsync();
        await fixture.Service.CreateAsync("UAT-01 WIP");

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync("UAT-01 wip"));

        StringAssert.Contains(ex.Message, "already exists");
        Assert.AreEqual(1, await fixture.Db.PipelineFeeds.CountAsync());
    }

    [TestMethod]
    public async Task UpdateAsync_ChangesDescription()
    {
        await using var fixture = await PipelineFeedServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Integration feed");

        var updated = await fixture.Service.UpdateAsync(created.Id, "Integration feed", "Shared integration packages");

        Assert.AreEqual("Shared integration packages", updated.Description);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesFeed()
    {
        await using var fixture = await PipelineFeedServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Temporary feed");

        await fixture.Service.DeleteAsync(created.Id);

        Assert.AreEqual(0, await fixture.Db.PipelineFeeds.CountAsync());
    }

    private sealed class PipelineFeedServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private PipelineFeedServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IPipelineFeedService service)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
        }

        public DevDashDbContext Db { get; }

        public IPipelineFeedService Service { get; }

        public static async Task<PipelineFeedServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IPipelineFeedService, PipelineFeedService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var service = serviceProvider.GetRequiredService<IPipelineFeedService>();
            return new PipelineFeedServiceFixture(serviceProvider, db, service);
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
