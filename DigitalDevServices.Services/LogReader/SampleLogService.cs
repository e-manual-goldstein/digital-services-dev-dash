using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public interface ISampleLogService
{
    IReadOnlyList<SampleLogDefinition> GetSamples();

    Task<IReadOnlyList<ParsedLogEntry>> LoadSampleAsync(string sampleId, CancellationToken cancellationToken = default);
}

public sealed class SampleLogService : ISampleLogService
{
    private static readonly IReadOnlyList<SampleLogDefinition> Samples =
    [
        new SampleLogDefinition
        {
            Id = "serilog-json",
            DisplayName = "Serilog JSON",
            FileName = "serilog-json.log",
            FormatName = "SerilogJson",
            Description = "One JSON object per line with @t, @l, and @mt fields."
        },
        new SampleLogDefinition
        {
            Id = "plain-text",
            DisplayName = "Plain text",
            FileName = "plain-text.log",
            FormatName = "PlainText",
            Description = "Single-line entries: timestamp, level, [logger], message."
        },
        new SampleLogDefinition
        {
            Id = "nlog-multiline",
            DisplayName = "NLog multiline",
            FileName = "nlog-multiline.log",
            FormatName = "NLogMultiline",
            Description = "Pipe-delimited headers with stack traces on following lines."
        },
        new SampleLogDefinition
        {
            Id = "log4net-pattern",
            DisplayName = "log4net pattern",
            FileName = "log4net-pattern.log",
            FormatName = "Log4NetPattern",
            Description = "Bracketed thread id and logger name; exception blocks span multiple lines."
        }
    ];

    private readonly LogParserRegistry _parserRegistry;
    private readonly string _sampleLogsDirectory;

    public SampleLogService(LogParserRegistry parserRegistry)
    {
        _parserRegistry = parserRegistry;
        _sampleLogsDirectory = Path.Combine(AppContext.BaseDirectory, "samples", "logs");
    }

    public IReadOnlyList<SampleLogDefinition> GetSamples() => Samples;

    public async Task<IReadOnlyList<ParsedLogEntry>> LoadSampleAsync(
        string sampleId,
        CancellationToken cancellationToken = default)
    {
        var sample = Samples.SingleOrDefault(item => item.Id.Equals(sampleId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Sample log '{sampleId}' was not found.");

        var filePath = Path.Combine(_sampleLogsDirectory, sample.FileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Sample log file was not found at '{filePath}'.");
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var parser = _parserRegistry.GetRequiredParser(sample.FormatName);
        return parser.Parse(content);
    }
}
