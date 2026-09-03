using DigitalDevServices.Data;
using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Services.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class DeployedPackageServiceTests
{
    [TestMethod]
    public async Task ScanAsync_ReturnsDllsFromPhysicalPath()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var packageDirectory = await fixture.CreatePackageDirectoryAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Customer Portal API");
        var environment = await fixture.CreateTrackedEnvironmentAsync(1);

        var instance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "1.0.0",
            PhysicalPath = packageDirectory
        });

        var result = await fixture.Service.ScanAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Packages);
        Assert.AreEqual("DigitalDevServices.Model.dll", result.Packages[0].FileName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Packages[0].AssemblyVersion));
    }

    [TestMethod]
    public async Task ScanAsync_ReturnsErrorWhenPhysicalPathMissing()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Reporting Service");
        var environment = await fixture.CreateTrackedEnvironmentAsync(2);

        var instance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "1.0.0"
        });

        var result = await fixture.Service.ScanAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "physical path");
    }

    [TestMethod]
    public async Task ScanAsync_ReturnsErrorWhenFolderDoesNotExist()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Admin Portal");
        var environment = await fixture.CreateTrackedEnvironmentAsync(3);

        var instance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "1.0.0",
            PhysicalPath = @"D:\does-not-exist\packages-test"
        });

        var result = await fixture.Service.ScanAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "does not exist");
    }

    [TestMethod]
    public async Task ScanAsync_ReturnsErrorWhenInstanceNotFound()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();

        var result = await fixture.Service.ScanAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "not found");
    }

    [TestMethod]
    public async Task ScanAsync_UsesManifestWhenPresent()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var packageDirectory = await fixture.CreatePackageDirectoryAsync();
        await fixture.WriteManifestAsync(
            packageDirectory,
            """
            "Path","Version"
            "DigitalDevServices.Model.dll","9.9.9.9"
            "Other.Library.dll","1.0.0.0"
            """);

        var application = await fixture.DeployableApplicationService.CreateAsync("Manifest App");
        var environment = await fixture.CreateTrackedEnvironmentAsync(4);
        var instance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "1.0.0",
            PhysicalPath = packageDirectory
        });

        var result = await fixture.Service.ScanAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(DeployedPackageSource.Manifest, result.Source);
        Assert.AreEqual(DeploymentManifestParser.ManifestFileName, result.ManifestFileName);
        Assert.HasCount(2, result.Packages);
        Assert.AreEqual("9.9.9.9", result.Packages.Single(package => package.FileName == "DigitalDevServices.Model.dll").AssemblyVersion);
    }

    [TestMethod]
    public async Task ScanAsync_FallsBackToFilesystemScanWhenManifestMissing()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var packageDirectory = await fixture.CreatePackageDirectoryAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Filesystem App");
        var environment = await fixture.CreateTrackedEnvironmentAsync(5);
        var instance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "1.0.0",
            PhysicalPath = packageDirectory
        });

        var result = await fixture.Service.ScanAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(DeployedPackageSource.FilesystemScan, result.Source);
        Assert.HasCount(1, result.Packages);
    }

    [TestMethod]
    public async Task ScanAsync_FallsBackToFilesystemScanWhenManifestHasNoEntries()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var packageDirectory = await fixture.CreatePackageDirectoryAsync();
        await fixture.WriteManifestAsync(
            packageDirectory,
            """
            "Path","Version"
            """);

        var application = await fixture.DeployableApplicationService.CreateAsync("Empty Manifest App");
        var environment = await fixture.CreateTrackedEnvironmentAsync(6);
        var instance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = environment.Id,
            BuildVersionNumber = "1.0.0",
            PhysicalPath = packageDirectory
        });

        var result = await fixture.Service.ScanAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(DeployedPackageSource.FilesystemScan, result.Source);
        Assert.HasCount(1, result.Packages);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("Fell back to filesystem scan", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task CompareInstancesAsync_ReturnsComparisonRowsForSameApplication()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var leftDirectory = await fixture.CreatePackageDirectoryAsync();
        var rightDirectory = await fixture.CreatePackageDirectoryAsync();
        await fixture.WriteManifestAsync(
            leftDirectory,
            """
            "Path","Version"
            "Shared.dll","1.0.0.0"
            "LeftOnly.dll","1.0.0.0"
            """);
        await fixture.WriteManifestAsync(
            rightDirectory,
            """
            "Path","Version"
            "Shared.dll","2.0.0.0"
            "RightOnly.dll","3.0.0.0"
            """);

        var application = await fixture.DeployableApplicationService.CreateAsync("Compare App");
        var leftEnvironment = await fixture.CreateTrackedEnvironmentAsync(10);
        var rightEnvironment = await fixture.CreateTrackedEnvironmentAsync(11);
        var leftInstance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = leftEnvironment.Id,
            BuildVersionNumber = "100",
            PhysicalPath = leftDirectory
        });
        var rightInstance = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = application.Id,
            EnvironmentId = rightEnvironment.Id,
            BuildVersionNumber = "200",
            PhysicalPath = rightDirectory
        });

        var result = await fixture.Service.CompareInstancesAsync(leftInstance.Id, rightInstance.Id);

        Assert.IsTrue(result.IsSuccess);
        var rows = result.Rows.ToDictionary(row => row.FileName);
        Assert.AreEqual(DeployedPackageComparisonStatus.Mismatch, rows["Shared.dll"].Status);
        Assert.AreEqual(DeployedPackageComparisonStatus.LeftOnly, rows["LeftOnly.dll"].Status);
        Assert.AreEqual(DeployedPackageComparisonStatus.RightOnly, rows["RightOnly.dll"].Status);
    }

    [TestMethod]
    public async Task CompareInstancesAsync_RejectsDifferentApplications()
    {
        await using var fixture = await DeployedPackageServiceFixture.CreateAsync();
        var directory = await fixture.CreatePackageDirectoryAsync();
        var applicationA = await fixture.DeployableApplicationService.CreateAsync("App A");
        var applicationB = await fixture.DeployableApplicationService.CreateAsync("App B");
        var environmentA = await fixture.CreateTrackedEnvironmentAsync(12);
        var environmentB = await fixture.CreateTrackedEnvironmentAsync(13);
        var instanceA = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = applicationA.Id,
            EnvironmentId = environmentA.Id,
            BuildVersionNumber = "1.0.0",
            PhysicalPath = directory
        });
        var instanceB = await fixture.ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
        {
            DeployableApplicationId = applicationB.Id,
            EnvironmentId = environmentB.Id,
            BuildVersionNumber = "1.0.0",
            PhysicalPath = directory
        });

        var result = await fixture.Service.CompareInstancesAsync(instanceA.Id, instanceB.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "same deployable application");
    }

    private sealed class DeployedPackageServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly string _tempRoot;

        private DeployedPackageServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            IDeployedPackageService service,
            IDeployableApplicationService deployableApplicationService,
            IApplicationInstanceService applicationInstanceService,
            string tempRoot)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            Service = service;
            DeployableApplicationService = deployableApplicationService;
            ApplicationInstanceService = applicationInstanceService;
            _tempRoot = tempRoot;
        }

        public DevDashDbContext Db { get; }

        public IDeployedPackageService Service { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public IApplicationInstanceService ApplicationInstanceService { get; }

        public static async Task<DeployedPackageServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
            services.AddScoped<IApplicationInstanceService, ApplicationInstanceService>();
            services.AddScoped<IDeployedPackageService, DeployedPackageService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var tempRoot = Path.Combine(Path.GetTempPath(), "devdash-packages-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            return new DeployedPackageServiceFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<IDeployedPackageService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                serviceProvider.GetRequiredService<IApplicationInstanceService>(),
                tempRoot);
        }

        public Task<string> CreatePackageDirectoryAsync()
        {
            var packageDirectory = Path.Combine(_tempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(packageDirectory);

            var sourceAssembly = typeof(DeployedPackageInfo).Assembly.Location;
            var targetPath = Path.Combine(packageDirectory, Path.GetFileName(sourceAssembly));
            File.Copy(sourceAssembly, targetPath, overwrite: true);

            return Task.FromResult(packageDirectory);
        }

        public Task WriteManifestAsync(string packageDirectory, string contents)
        {
            var manifestPath = Path.Combine(packageDirectory, DeploymentManifestParser.ManifestFileName);
            File.WriteAllText(manifestPath, contents);
            return Task.CompletedTask;
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

        public async ValueTask DisposeAsync()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }

            await _serviceProvider.DisposeAsync();
        }
    }
}
