namespace DigitalDevServices.Model.Logs;

public class LogFormatProfileUpsert
{
    public required Guid DeployableApplicationId { get; init; }

    public required string FormatName { get; init; }

    public string? ParserConfig { get; init; }

    public string? Notes { get; init; }
}
