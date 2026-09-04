using DigitalDevServices.Data;
using DigitalDevServices.Model.GitHistory;
using DigitalDevServices.Services.GitHistory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class GitRepositoryServiceTests
{
    [TestMethod]
    public async Task CreateAsync_PersistsRepository()
    {
        await using var fixture = await GitRepositoryServiceFixture.CreateAsync();

        var created = await fixture.Service.CreateAsync(new GitRepositoryUpsert
        {
            Name = "Customer Portal"
        });

        var loaded = await fixture.Service.GetByIdAsync(created.Id);

        Assert.IsNotNull(loaded);
        Assert.AreEqual("Customer Portal", loaded!.Name);
        Assert.HasCount(0, loaded.ArtifactComponents);
    }

    [TestMethod]
    public async Task CreateAsync_RejectsDuplicateName()
    {
        await using var fixture = await GitRepositoryServiceFixture.CreateAsync();

        await fixture.Service.CreateAsync(new GitRepositoryUpsert
        {
            Name = "Shared Library"
        });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            fixture.Service.CreateAsync(new GitRepositoryUpsert
            {
                Name = "shared library"
            }));
    }

    [TestMethod]
    public async Task CreateComponentAsync_PersistsComponent()
    {
        await using var fixture = await GitRepositoryServiceFixture.CreateAsync();

        var repository = await fixture.Service.CreateAsync(new GitRepositoryUpsert
        {
            Name = "Payments Monorepo"
        });

        var component = await fixture.Service.CreateComponentAsync(repository.Id, new ArtifactComponentUpsert
        {
            Name = "Payments API",
            DateMigrated = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
            CurrentLocationUrl = "https://dev.azure.com/org/project/_git/payments-api"
        });

        var loaded = await fixture.Service.GetByIdAsync(repository.Id);

        Assert.IsNotNull(loaded);
        Assert.HasCount(1, loaded!.ArtifactComponents);
        Assert.AreEqual(component.Id, loaded.ArtifactComponents.Single().Id);
        Assert.AreEqual("https://dev.azure.com/org/project/_git/payments-api", loaded.ArtifactComponents.Single().CurrentLocationUrl);
    }

    [TestMethod]
    public async Task AddHistoricRecordAsync_UpdatesLastLocationDerivation()
    {
        await using var fixture = await GitRepositoryServiceFixture.CreateAsync();

        var repository = await fixture.Service.CreateAsync(new GitRepositoryUpsert
        {
            Name = "Payments Monorepo"
        });

        var component = await fixture.Service.CreateComponentAsync(repository.Id, new ArtifactComponentUpsert
        {
            Name = "Payments API",
            DateMigrated = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
            CurrentLocationUrl = "https://dev.azure.com/org/project/_git/payments-api"
        });

        await fixture.Service.AddHistoricRecordAsync(component.Id, new HistoricGitRepoRecordUpsert
        {
            Name = "Monolith",
            LastLocationUrl = "https://dev.azure.com/org/project/_git/monolith",
            DateMigrated = new DateTimeOffset(2020, 3, 1, 0, 0, 0, TimeSpan.Zero)
        });
        await fixture.Service.AddHistoricRecordAsync(component.Id, new HistoricGitRepoRecordUpsert
        {
            Name = "Payments Service",
            LastLocationUrl = "https://dev.azure.com/org/project/_git/payments-service",
            DateMigrated = new DateTimeOffset(2023, 8, 10, 0, 0, 0, TimeSpan.Zero)
        });

        var loaded = await fixture.Service.GetComponentByIdAsync(component.Id);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(
            "https://dev.azure.com/org/project/_git/payments-service",
            ArtifactComponentDisplay.GetLastLocationUrl(loaded!));
        Assert.HasCount(2, loaded!.PreviousLocations);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesRepositoryComponentsAndHistoricRecords()
    {
        await using var fixture = await GitRepositoryServiceFixture.CreateAsync();

        var repository = await fixture.Service.CreateAsync(new GitRepositoryUpsert
        {
            Name = "Legacy Web"
        });
        var component = await fixture.Service.CreateComponentAsync(repository.Id, new ArtifactComponentUpsert
        {
            Name = "Web UI",
            DateMigrated = DateTimeOffset.UtcNow,
            CurrentLocationUrl = "https://dev.azure.com/org/project/_git/legacy-web"
        });
        await fixture.Service.AddHistoricRecordAsync(component.Id, new HistoricGitRepoRecordUpsert
        {
            Name = "Monolith",
            LastLocationUrl = "https://dev.azure.com/org/project/_git/monolith",
            DateMigrated = DateTimeOffset.UtcNow.AddYears(-5)
        });

        await fixture.Service.DeleteAsync(repository.Id);

        Assert.IsNull(await fixture.Service.GetByIdAsync(repository.Id));
        Assert.AreEqual(0, await fixture.Db.HistoricGitRepoRecords.CountAsync());
        Assert.AreEqual(0, await fixture.Db.ArtifactComponents.CountAsync());
    }

    private sealed class GitRepositoryServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private GitRepositoryServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IGitRepositoryService service)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
        }

        public DevDashDbContext Db { get; }

        public IGitRepositoryService Service { get; }

        public static async Task<GitRepositoryServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IGitRepositoryService, GitRepositoryService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            return new GitRepositoryServiceFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IGitRepositoryService>());
        }

        public async ValueTask DisposeAsync() => await _serviceProvider.DisposeAsync();
    }
}
