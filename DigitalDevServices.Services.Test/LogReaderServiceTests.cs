using DigitalDevServices.Data;
using DigitalDevServices.Model.Applications;
using DigitalDevServices.Model.Entities;
using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class LogReaderServiceTests
{
    [TestMethod]
    public async Task ReadAsync_ReturnsParsedEntriesFromLogFile()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var logFilePath = Path.Combine(logDirectory, "app.log");
        await File.WriteAllTextAsync(logFilePath, """
            2026-08-23 09:02:11.004 INFO  [WorkerHost] Background worker started
            2026-08-23 09:03:01.774 WARN  [ImportJob] Row 88 skipped
            2026-08-23 09:05:17.889 ERROR [EmailSender] SMTP handshake failed
            """);

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logFilePath);
        var result = await fixture.ReaderService.ReadAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(logFilePath, result.LogFilePath);
        Assert.HasCount(3, result.Entries);
        StringAssert.Contains(result.RawContent!, "Background worker started");
        Assert.AreEqual("INFO", result.Entries[0].Level);
        Assert.IsNotNull(result.Entries[0].Timestamp);
        Assert.AreEqual("WARN", result.Entries[1].Level);
        Assert.AreEqual("ERROR", result.Entries[2].Level);
    }

    [TestMethod]
    public async Task ReadAsync_LimitsToLastMaxLines()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var logFilePath = Path.Combine(logDirectory, "app.log");

        var lines = Enumerable.Range(1, 120)
            .Select(index => $"2026-08-23 09:02:{index % 60:00}.004 INFO  [WorkerHost] Line {index}");
        await File.WriteAllLinesAsync(logFilePath, lines);

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logFilePath);
        var result = await fixture.ReaderService.ReadAsync(instance.Id, maxLines: 100);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.RawLinesRead);
        Assert.HasCount(100, result.Entries);
        Assert.AreEqual("Line 21", result.Entries[0].Message);
        Assert.AreEqual("Line 120", result.Entries[^1].Message);
    }

    [TestMethod]
    public async Task ReadAsync_UsesNewestLogInDirectory()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var olderLogPath = Path.Combine(logDirectory, "older.log");
        var newerLogPath = Path.Combine(logDirectory, "newer.log");

        await File.WriteAllTextAsync(olderLogPath, "2026-08-23 09:00:00.000 INFO  [WorkerHost] Old entry");
        await File.WriteAllTextAsync(newerLogPath, "2026-08-23 09:10:00.000 WARN  [WorkerHost] New entry");
        File.SetLastWriteTimeUtc(olderLogPath, DateTime.UtcNow.AddHours(-2));
        File.SetLastWriteTimeUtc(newerLogPath, DateTime.UtcNow);

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logDirectory);
        var result = await fixture.ReaderService.ReadAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newerLogPath, result.LogFilePath);
        Assert.HasCount(1, result.Entries);
        Assert.AreEqual("WARN", result.Entries[0].Level);
        Assert.AreEqual("New entry", result.Entries[0].Message);
    }

    [TestMethod]
    public async Task ReadAsync_ReturnsErrorWhenLogPathMissing()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logPath: null);
        var result = await fixture.ReaderService.ReadAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "log path");
    }

    [TestMethod]
    public async Task ReadAsync_ReturnsErrorWhenPathDoesNotExist()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, @"D:\does-not-exist\app.log");

        var result = await fixture.ReaderService.ReadAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "does not exist");
    }

    [TestMethod]
    public async Task ReadAsync_ReturnsErrorWhenInstanceNotFound()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();

        var result = await fixture.ReaderService.ReadAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "not found");
    }

    [TestMethod]
    public async Task ReadAsync_ReturnsErrorWhenLogFormatProfileMissing()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var logFilePath = Path.Combine(logDirectory, "app.log");
        await File.WriteAllTextAsync(logFilePath, "2026-08-23 09:02:11.004 INFO  [WorkerHost] Background worker started");

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logFilePath);
        var result = await fixture.ReaderService.ReadAsync(instance.Id);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "log format profile");
        StringAssert.Contains(result.RawContent!, "Background worker started");
    }

    [TestMethod]
    public async Task ListLogFilesAsync_ReturnsNewestFirstForDirectory()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var olderLogPath = Path.Combine(logDirectory, "older.log");
        var newerLogPath = Path.Combine(logDirectory, "newer.log");

        await File.WriteAllTextAsync(olderLogPath, "old");
        await File.WriteAllTextAsync(newerLogPath, "new");
        File.SetLastWriteTimeUtc(olderLogPath, DateTime.UtcNow.AddHours(-2));
        File.SetLastWriteTimeUtc(newerLogPath, DateTime.UtcNow);

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logDirectory);
        var result = await fixture.ReaderService.ListLogFilesAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsDirectory);
        Assert.HasCount(2, result.Files);
        Assert.AreEqual("newer.log", result.Files[0].FileName);
        Assert.AreEqual("older.log", result.Files[1].FileName);
        Assert.IsGreaterThan(0, result.Files[0].SizeBytes);
    }

    [TestMethod]
    public async Task ListLogFilesAsync_IncludesTxtFilesInDirectory()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var logPath = Path.Combine(logDirectory, "app.log");
        var txtPath = Path.Combine(logDirectory, "service.txt");

        await File.WriteAllTextAsync(logPath, "log");
        await File.WriteAllTextAsync(txtPath, "txt");
        File.SetLastWriteTimeUtc(logPath, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(txtPath, DateTime.UtcNow);

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logDirectory);
        var result = await fixture.ReaderService.ListLogFilesAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Files);
        Assert.AreEqual("service.txt", result.Files[0].FileName);
        Assert.AreEqual("app.log", result.Files[1].FileName);
    }

    [TestMethod]
    public async Task ReadAsync_ReadsExplicitTxtFileInDirectory()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var txtPath = Path.Combine(logDirectory, "worker.txt");

        await File.WriteAllTextAsync(txtPath, "2026-08-23 09:00:00.000 INFO  [WorkerHost] Text log entry");

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logDirectory);
        var result = await fixture.ReaderService.ReadAsync(instance.Id, logFilePath: txtPath);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(txtPath, result.LogFilePath);
        Assert.HasCount(1, result.Entries);
        Assert.AreEqual("Text log entry", result.Entries[0].Message);
    }

    [TestMethod]
    public async Task ListLogFilesAsync_ReturnsSingleFileWhenPathIsFile()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var logFilePath = Path.Combine(logDirectory, "app.log");
        await File.WriteAllTextAsync(logFilePath, "entry");

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logFilePath);
        var result = await fixture.ReaderService.ListLogFilesAsync(instance.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsDirectory);
        Assert.HasCount(1, result.Files);
        Assert.AreEqual(logFilePath, result.Files[0].FilePath);
    }

    [TestMethod]
    public async Task ReadAsync_ReadsExplicitLogFileInDirectory()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var olderLogPath = Path.Combine(logDirectory, "older.log");
        var newerLogPath = Path.Combine(logDirectory, "newer.log");

        await File.WriteAllTextAsync(olderLogPath, "2026-08-23 09:00:00.000 INFO  [WorkerHost] Old entry");
        await File.WriteAllTextAsync(newerLogPath, "2026-08-23 09:10:00.000 WARN  [WorkerHost] New entry");
        File.SetLastWriteTimeUtc(olderLogPath, DateTime.UtcNow.AddHours(-2));
        File.SetLastWriteTimeUtc(newerLogPath, DateTime.UtcNow);

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logDirectory);
        var result = await fixture.ReaderService.ReadAsync(instance.Id, logFilePath: olderLogPath);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(olderLogPath, result.LogFilePath);
        Assert.HasCount(1, result.Entries);
        Assert.AreEqual("Old entry", result.Entries[0].Message);
    }

    [TestMethod]
    public async Task ReadAsync_RejectsLogFileOutsideConfiguredDirectory()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var otherDirectory = await fixture.CreateLogDirectoryAsync();
        var logFilePath = Path.Combine(logDirectory, "app.log");
        var outsideLogPath = Path.Combine(otherDirectory, "outside.log");

        await File.WriteAllTextAsync(logFilePath, "2026-08-23 09:00:00.000 INFO  [WorkerHost] Inside");
        await File.WriteAllTextAsync(outsideLogPath, "2026-08-23 09:00:00.000 INFO  [WorkerHost] Outside");

        var application = await fixture.DeployableApplicationService.CreateAsync("Worker Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logDirectory);
        var result = await fixture.ReaderService.ReadAsync(instance.Id, logFilePath: outsideLogPath);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "not in the configured log directory");
    }

    [TestMethod]
    public async Task ReadAsync_UsesFormatOverrideInsteadOfConfiguredProfile()
    {
        await using var fixture = await LogReaderServiceFixture.CreateAsync();
        var logDirectory = await fixture.CreateLogDirectoryAsync();
        var logFilePath = Path.Combine(logDirectory, "app.log");
        await File.WriteAllTextAsync(logFilePath, """
            {"@t":"2026-08-23T08:15:02.1123456+01:00","@l":"Information","@mt":"Application starting"}
            """);

        var application = await fixture.DeployableApplicationService.CreateAsync("API Host");
        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.PlainText
        });

        var instance = await fixture.CreateApplicationInstanceAsync(application.Id, logFilePath);
        var wrongProfileResult = await fixture.ReaderService.ReadAsync(instance.Id);
        var overrideResult = await fixture.ReaderService.ReadAsync(
            instance.Id,
            formatName: LogFormatNames.SerilogJson);

        Assert.IsTrue(wrongProfileResult.IsSuccess);
        Assert.IsEmpty(wrongProfileResult.Entries);
        Assert.IsTrue(overrideResult.IsSuccess);
        Assert.HasCount(1, overrideResult.Entries);
        Assert.AreEqual("Information", overrideResult.Entries[0].Level);
        Assert.AreEqual("Application starting", overrideResult.Entries[0].Message);
    }

    private sealed class LogReaderServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly string _tempRoot;

        private LogReaderServiceFixture(
            ServiceProvider serviceProvider,
            DevDashDbContext db,
            ILogReaderService readerService,
            IDeployableApplicationService deployableApplicationService,
            ILogFormatProfileService profileService,
            IApplicationInstanceService applicationInstanceService,
            string tempRoot)
        {
            _serviceProvider = serviceProvider;
            Db = db;
            ReaderService = readerService;
            DeployableApplicationService = deployableApplicationService;
            ProfileService = profileService;
            ApplicationInstanceService = applicationInstanceService;
            _tempRoot = tempRoot;
        }

        public DevDashDbContext Db { get; }

        public ILogReaderService ReaderService { get; }

        public IDeployableApplicationService DeployableApplicationService { get; }

        public ILogFormatProfileService ProfileService { get; }

        public IApplicationInstanceService ApplicationInstanceService { get; }

        public static async Task<LogReaderServiceFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<DevDashDbContext>(options => options.UseSqlite("Data Source=:memory:"));
            services.AddScoped<IDeployableApplicationService, DeployableApplicationService>();
            services.AddScoped<IApplicationInstanceService, ApplicationInstanceService>();
            services.AddSingleton<ILogEntryParser, PlainTextLogParser>();
            services.AddSingleton<ILogEntryParser, SerilogJsonLogParser>();
            services.AddSingleton<ILogEntryParser, NLogMultilineLogParser>();
            services.AddSingleton<ILogEntryParser, Log4NetPatternLogParser>();
            services.AddSingleton<LogParserRegistry>();
            services.AddSingleton<CustomRegexLogParser>();
            services.AddScoped<ILogFormatProfileService, LogFormatProfileService>();
            services.AddScoped<ILogParsingService, LogParsingService>();
            services.AddScoped<ILogReaderService, LogReaderService>();

            var serviceProvider = services.BuildServiceProvider();
            var db = serviceProvider.GetRequiredService<DevDashDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var tempRoot = Path.Combine(Path.GetTempPath(), "devdash-logs-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            return new LogReaderServiceFixture(
                serviceProvider,
                db,
                serviceProvider.GetRequiredService<ILogReaderService>(),
                serviceProvider.GetRequiredService<IDeployableApplicationService>(),
                serviceProvider.GetRequiredService<ILogFormatProfileService>(),
                serviceProvider.GetRequiredService<IApplicationInstanceService>(),
                tempRoot);
        }

        public Task<string> CreateLogDirectoryAsync()
        {
            var logDirectory = Path.Combine(_tempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(logDirectory);
            return Task.FromResult(logDirectory);
        }

        public async Task<ApplicationInstance> CreateApplicationInstanceAsync(Guid deployableApplicationId, string? logPath)
        {
            var environment = new TrackedEnvironment
            {
                Id = Guid.NewGuid(),
                RemoteId = Random.Shared.Next(1, 10000),
                DateLastUpdated = DateTimeOffset.UtcNow
            };

            Db.TrackedEnvironments.Add(environment);
            await Db.SaveChangesAsync();

            return await ApplicationInstanceService.UpsertAsync(new ApplicationInstanceUpsert
            {
                DeployableApplicationId = deployableApplicationId,
                EnvironmentId = environment.Id,
                BuildVersionNumber = "1.0.0",
                LogPath = logPath
            });
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
