using DigitalDevServices.Data;
using DigitalDevServices.Model.Logs;
using DigitalDevServices.Services.Applications;
using DigitalDevServices.Services.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class CustomRegexLogParserTests
{
    private static readonly CustomRegexLogParser Parser = new();

    private const string PlainTextPattern =
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+(?<level>[A-Z]+)\s+\[(?<logger>[^\]]+)\]\s+(?<message>.*)$";

    private const string NLogEntryStartPattern =
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d+)?)\|(?<level>[A-Z]+)\|(?<logger>[^|]+)\|(?<message>.*)$";

    [TestMethod]
    public void Parse_EntryMode_ParsesMatchingLines()
    {
        const string content = """
            2026-08-23 09:02:11.004 INFO  [WorkerHost] Background worker started
            not a log line
            2026-08-23 09:03:01.774 WARN  [ImportJob] Row 88 skipped
            """;

        var entries = Parser.Parse(content, new CustomRegexParserConfig
        {
            Mode = CustomRegexParserConfig.ModeEntry,
            Pattern = PlainTextPattern
        });

        Assert.HasCount(2, entries);
        Assert.AreEqual("INFO", entries[0].Level);
        Assert.AreEqual("Background worker started", entries[0].Message);
        Assert.AreEqual("WorkerHost", entries[0].Properties!["logger"]);
        Assert.AreEqual("WARN", entries[1].Level);
    }

    [TestMethod]
    public void Parse_EntryStartMode_AppendsContinuationLines()
    {
        const string content = """
            2026-08-23 09:05:17.889|ERROR|EmailSender|SMTP handshake failed
               at MailClient.Connect()
               at EmailSender.Send()
            2026-08-23 09:06:01.100|INFO|EmailSender|Retry queued
            """;

        var entries = Parser.Parse(content, new CustomRegexParserConfig
        {
            Mode = CustomRegexParserConfig.ModeEntryStart,
            Pattern = NLogEntryStartPattern
        });

        Assert.HasCount(2, entries);
        Assert.AreEqual("ERROR", entries[0].Level);
        StringAssert.Contains(entries[0].Message, "SMTP handshake failed");
        StringAssert.Contains(entries[0].Message, "MailClient.Connect()");
        StringAssert.Contains(entries[0].RawText, "EmailSender.Send()");
        Assert.AreEqual("INFO", entries[1].Level);
    }

    [TestMethod]
    public void Validate_RejectsPatternWithoutMessageGroup()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CustomRegexParserConfigValidator.Validate(new CustomRegexParserConfig
            {
                Mode = CustomRegexParserConfig.ModeEntry,
                Pattern = @"^(?<level>[A-Z]+)\s+(?<text>.*)$"
            }));

        StringAssert.Contains(ex.Message, "message");
    }

    [TestMethod]
    public async Task ParseForDeployableApplicationAsync_UsesSavedCustomRegexProfile()
    {
        await using var fixture = await LogServicesFixture.CreateAsync();
        var application = await fixture.DeployableApplicationService.CreateAsync("Custom Worker");

        var parserConfig = new CustomRegexParserConfig
        {
            Mode = CustomRegexParserConfig.ModeEntry,
            Pattern = PlainTextPattern
        };

        await fixture.ProfileService.UpsertAsync(new LogFormatProfileUpsert
        {
            DeployableApplicationId = application.Id,
            FormatName = LogFormatNames.CustomRegex,
            ParserConfig = parserConfig.ToJson()
        });

        const string content = "2026-08-23 09:02:11.004 INFO  [WorkerHost] Background worker started";
        var entries = await fixture.ParsingService.ParseForDeployableApplicationAsync(application.Id, content);

        Assert.HasCount(1, entries);
        Assert.AreEqual("Background worker started", entries[0].Message);
    }

    private sealed class LogServicesFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private LogServicesFixture(
            ServiceProvider serviceProvider,
            ILogFormatProfileService profileService,
            IDeployableApplicationService deployableApplicationService,
            ILogParsingService parsingService)
        {
            _serviceProvider = serviceProvider;
            ProfileService = profileService;
            DeployableApplicationService = deployableApplicationService;
            ParsingService = parsingService;
        }

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
