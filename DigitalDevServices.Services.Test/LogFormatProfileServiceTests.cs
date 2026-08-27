using DigitalDevServices.Data;
using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class LogFormatProfileServiceTests
{
    [TestMethod]
    public async Task UpsertAsync_PersistsProfileForDeployableApplication()
    {
        await using var fixture = await LogServicesFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Customer Portal API");

        var profile = await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText,
            Notes = "Standard worker logs"
        });

        Assert.AreEqual(LogFormatNames.PlainText, profile.FormatName);
        Assert.AreEqual("Standard worker logs", profile.Notes);

        var loaded = await fixture.ProfileService.GetByDeployableApplicationIdAsync(application.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(profile.Id, loaded!.Id);
    }

    [TestMethod]
    public async Task UpsertAsync_RejectsUnknownFormatName()
    {
        await using var fixture = await LogServicesFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Reporting Service");

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
            {
                DeployableApplicationId = application.Id,
                FormatName = "UnknownFormat"
            }));

        StringAssert.Contains(ex.Message, "not supported");
    }

    [TestMethod]
    public async Task DeleteByDeployableApplicationIdAsync_RemovesProfile()
    {
        await using var fixture = await LogServicesFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Admin Portal");

        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.SerilogJson
        });

        await fixture.ProfileService.DeleteByDeployableApplicationIdAsync(application.Id);

        Assert.IsNull(await fixture.ProfileService.GetByDeployableApplicationIdAsync(application.Id));
    }

    [TestMethod]
    public async Task ParseForDeployableApplicationAsync_UsesAssignedPlainTextProfile()
    {
        await using var fixture = await LogServicesFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");

        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        const string content = """
            2026-08-23 09:02:11.004 INFO  [WorkerHost] Background worker started
            2026-08-23 09:03:01.774 WARN  [ImportJob] Row 88 skipped
            """;

        var entries = await fixture.ParsingService.ParseForDeployableApplicationAsync(application.Id, content);

        Assert.HasCount(2, entries);
        Assert.AreEqual("INFO", entries[0].Level);
        Assert.AreEqual("Background worker started", entries[0].Message);
        Assert.AreEqual("WARN", entries[1].Level);
    }

    [TestMethod]
    public async Task ParseForDeployableApplicationAsync_ThrowsWhenProfileMissing()
    {
        await using var fixture = await LogServicesFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("No Profile App");

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            fixture.ParsingService.ParseForDeployableApplicationAsync(application.Id, "line"));

        StringAssert.Contains(ex.Message, "No log format profile");
    }

    internal sealed class LogServicesFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private LogServicesFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            ILogFormatProfileService profileService,
            IDeployableApplicationService deployableApplicationService,
            ILogParsingService parsingService)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            ProfileService = profileService;
            DeployableApplicationService = deployableApplicationService;
            ParsingService = parsingService;
        }

        public DevDashDbContext Db { get; }

        public ILogFormatProfileService ProfileService { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public ILogParsingService ParsingService { get; }

        public static async Task<LogServicesFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
            services.AddSingleton<ILogEntryParser, PlainTextLogParser>();
            services.AddSingleton<ILogEntryParser, SerilogJsonLogParser>();
            services.AddSingleton<ILogEntryParser, NLogMultilineLogParser>();
            services.AddSingleton<ILogEntryParser, Log4NetPatternLogParser>();
            services.AddSingleton<LogParserRegistry>();
            services.AddSingleton<CustomRegexLogParser>();
            services.AddScoped<ILogFormatProfileService, LogFormatProfileService>();
            services.AddScoped<ILogParsingService, LogParsingService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            return new LogServicesFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<ILogFormatProfileService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                serviceProvider.GetRequiredService<ILogParsingService>());
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
