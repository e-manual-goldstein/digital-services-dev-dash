using DigitalDevServices.Data;
using DigitalDevServices.Services.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class DeployableApplicationServiceTests
{
    [TestMethod]
    public async Task CreateAsync_PersistsApplicationAndReadsBackByName()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync(
            "Customer Portal API",
            projectKey: "customer-portal-api",
            notes: "Public-facing API");

        var loaded = await fixture.Service.GetByNameAsync("Customer Portal API");

        Assert.IsNotNull(loaded);
        Assert.AreEqual(created.Id, loaded!.Id);
        Assert.AreEqual("customer-portal-api", loaded.ProjectKey);
        Assert.AreEqual("Public-facing API", loaded.Notes);
    }

    [TestMethod]
    public async Task CreateAsync_RejectsDuplicateName()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        await fixture.Service.CreateAsync("Reporting Service");

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync("reporting service"));

        StringAssert.Contains(ex.Message, "already exists");
        Assert.AreEqual(1, await fixture.Db.DeployableApplications.CountAsync());
    }

    [TestMethod]
    public async Task UpdateAsync_ChangesName()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Legacy Admin");

        var updated = await fixture.Service.UpdateAsync(created.Id, "Admin Portal");

        Assert.AreEqual("Admin Portal", updated.Name);
    }

    [TestMethod]
    public async Task CreateAsync_PersistsIsWebAppFlag()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Customer Portal", isWebApp: true);

        var loaded = await fixture.Service.GetByIdAsync(created.Id);

        Assert.IsNotNull(loaded);
        Assert.IsTrue(loaded!.IsWebApp);
    }

    [TestMethod]
    public async Task UpdateAsync_ChangesIsWebAppFlag()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Reporting Service");

        var updated = await fixture.Service.UpdateAsync(created.Id, "Reporting Service", isWebApp: true);

        Assert.IsTrue(updated.IsWebApp);
    }

    [TestMethod]
    public async Task CreateAsync_PersistsPathToLogFilesTemplate()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync(
            "Customer Portal",
            pathToLogFiles: @"{MachineName}\{EnvironmentCode}\{AppName}\Logs");

        var loaded = await fixture.Service.GetByIdAsync(created.Id);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(@"{MachineName}\{EnvironmentCode}\{AppName}\Logs", loaded!.PathToLogFiles);
    }

    [TestMethod]
    public async Task UpdateAsync_ChangesPathToLogFilesTemplate()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Reporting Service");

        var updated = await fixture.Service.UpdateAsync(
            created.Id,
            "Reporting Service",
            pathToLogFiles: @"{MachineName}\Logs");

        Assert.AreEqual(@"{MachineName}\Logs", updated.PathToLogFiles);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesApplication()
    {
        await using var fixture = await DeployableApplicationServiceFixture.CreateAsync();
        var created = await fixture.Service.CreateAsync("Temporary App");

        await fixture.Service.DeleteAsync(created.Id);

        Assert.AreEqual(0, await fixture.Db.DeployableApplications.CountAsync());
    }

    private sealed class DeployableApplicationServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private DeployableApplicationServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IDeployableApplicationService service)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
        }

        public DevDashDbContext Db { get; }

        public IDeployableApplicationService Service { get; }

        public static async Task<DeployableApplicationServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var service = serviceProvider.GetRequiredService<IDeployableApplicationService>();
            return new DeployableApplicationServiceFixture(serviceProvider, db, service);
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
